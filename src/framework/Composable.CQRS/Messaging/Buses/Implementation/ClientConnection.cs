using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.NetMQCE;
using Composable.System;
using Composable.System.Collections.Collections;
using Composable.System.Threading;
using Composable.System.Threading.ResourceAccess;
using NetMQ;
using NetMQ.Sockets;

namespace Composable.Messaging.Buses.Implementation
{
    class ClientConnection : IClientConnection
    {
        public async Task DispatchAsync(ITransactionalExactlyOnceDeliveryEvent @event) => await _state.WithExclusiveAccess(async state => await DispatchMessageAsync(@event, state, TransportMessage.OutGoing.Create(@event)));

        public async Task DispatchAsync(ITransactionalExactlyOnceDeliveryCommand command) => await _state.WithExclusiveAccess(async state => await DispatchMessageAsync(command, state, TransportMessage.OutGoing.Create(command)));

        public async Task<Task<TCommandResult>> DispatchAsyncAsync<TCommandResult>(ITransactionalExactlyOnceDeliveryCommand<TCommandResult> command) => await _state.WithExclusiveAccess(async state =>
        {
            var taskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            var outGoingMessage = TransportMessage.OutGoing.Create(command);

            state.ExpectedResponseTasks.Add(outGoingMessage.MessageId, taskCompletionSource);

            await DispatchMessageAsync(command, state, outGoingMessage);

            return await Task.FromResult(taskCompletionSource.Task.Cast<object, TCommandResult>());
        });

        public async Task<TQueryResult> DispatchAsync<TQueryResult>(IQuery<TQueryResult> query) => (TQueryResult)await _state.WithExclusiveAccess(state =>
        {
            var taskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            var outGoingMessage = TransportMessage.OutGoing.Create(query);

            state.ExpectedResponseTasks.Add(outGoingMessage.MessageId, taskCompletionSource);
            state.GlobalBusStateTracker.SendingMessageOnTransport(outGoingMessage, query);
            state.DispatchQueue.Enqueue(outGoingMessage);

            return taskCompletionSource.Task;
        });

        async Task DispatchMessageAsync(ITransactionalExactlyOnceDeliveryMessage message, State @this, TransportMessage.OutGoing outGoingMessage)
        {
            await @this.MessageStorage.MarkAsSentAsync(outGoingMessage);
            //todo: after transaction succeeds...
            @this.PendingDeliveryNotifications.Add(outGoingMessage.MessageId, new PendingDeliveryNotification(outGoingMessage.MessageId, @this.TimeSource.UtcNow));

            @this.GlobalBusStateTracker.SendingMessageOnTransport(outGoingMessage, message);
            @this.DispatchQueue.Enqueue(outGoingMessage);
        }

        public ClientConnection(IGlobalBusStateTracker globalBusStateTracker,
                                IEndpoint endpoint,
                                NetMQPoller poller,
                                IUtcTimeTimeSource timeSource,
                                InterprocessTransport.MessageStorage messageStorage)
        {
            _state.WithExclusiveAccess(state =>
            {
                state.TimeSource = timeSource;

                state.MessageStorage = messageStorage;

                state.GlobalBusStateTracker = globalBusStateTracker;

                state.Poller = poller;

                state.Poller.Add(state.DispatchQueue);

                state.DispatchQueue.ReceiveReady += state.DispatchMessage;

                state.Socket = new DealerSocket();

                //Should we screw up with the pipelining we prefer performance problems (memory usage) to lost messages or blocking
                state.Socket.Options.SendHighWatermark = int.MaxValue;
                state.Socket.Options.ReceiveHighWatermark = int.MaxValue;

                //We guarantee delivery upon restart in other ways. When we shut down, just do it.
                state.Socket.Options.Linger = 0.Milliseconds();

                state.Socket.ReceiveReady += ReceiveResponse;

                state.Socket.Connect(endpoint.Address);
                poller.Add(state.Socket);
            });
        }

        public void Dispose() => _state.WithExclusiveAccess(state =>
        {
            state.Socket.Dispose();
            state.DispatchQueue.Dispose();
        });

        class State
        {
            internal IGlobalBusStateTracker GlobalBusStateTracker;
            internal readonly Dictionary<Guid, TaskCompletionSource<object>> ExpectedResponseTasks = new Dictionary<Guid, TaskCompletionSource<object>>();
            internal readonly Dictionary<Guid, PendingDeliveryNotification> PendingDeliveryNotifications = new Dictionary<Guid, PendingDeliveryNotification>();
            internal DealerSocket Socket;
            internal NetMQPoller Poller;
            internal readonly NetMQQueue<TransportMessage.OutGoing> DispatchQueue = new NetMQQueue<TransportMessage.OutGoing>();
            internal IUtcTimeTimeSource TimeSource { get; set; }
            internal InterprocessTransport.MessageStorage MessageStorage { get; set; }

            internal void DispatchMessage(object sender, NetMQQueueEventArgs<TransportMessage.OutGoing> e)
            {
                while(e.Queue.TryDequeue(out var message, TimeSpan.Zero))
                {
                    Socket.Send(message);
                }
            }
        }

        readonly IThreadShared<State> _state = ThreadShared<State>.WithTimeout(10.Seconds());

        //Runs on poller thread so NO BLOCKING HERE!
        void ReceiveResponse(object sender, NetMQSocketEventArgs e)
        {
            var responseBatch = TransportMessage.Response.Incoming.ReceiveBatch(e.Socket, batchMaximum: 100);

            _state.WithExclusiveAccess(state =>
            {
                foreach(var response in responseBatch)
                {
                    switch(response.ResponseType)
                    {
                        case TransportMessage.Response.ResponseType.Success:
                            var successResponse = state.ExpectedResponseTasks.GetAndRemove(response.RespondingToMessageId);
                            Task.Run(() =>
                            {
                                try
                                {
                                    successResponse.SetResult(response.DeserializeResult());
                                }
                                catch(Exception exception)
                                {
                                    successResponse.SetException(exception);
                                }
                            });
                            break;
                        case TransportMessage.Response.ResponseType.Failure:
                            var failureResponse = state.ExpectedResponseTasks.GetAndRemove(response.RespondingToMessageId);
                            failureResponse.SetException(new MessageDispatchingFailedException());
                            break;
                        case TransportMessage.Response.ResponseType.Received:
#pragma warning disable 4014
                            Contract.Result.Assert(state.PendingDeliveryNotifications.Remove(response.RespondingToMessageId));
                            state.MessageStorage.MarkAsReceivedAsync(response);
#pragma warning restore 4014
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            });
        }

        class PendingDeliveryNotification
        {
            internal PendingDeliveryNotification(Guid messageId, DateTime sentAt)
            {
                MessageId = messageId;
                SentAt = sentAt;
            }

            internal Guid MessageId { get; }
            internal DateTime SentAt { get; }
        }
    }
}
