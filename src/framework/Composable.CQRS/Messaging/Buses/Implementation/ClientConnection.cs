using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.NetMQCE;
using Composable.System;
using Composable.System.Collections.Collections;
using Composable.System.Threading.ResourceAccess;
using NetMQ;
using NetMQ.Sockets;

namespace Composable.Messaging.Buses.Implementation
{
    class ClientConnection : IClientConnection
    {
        public void Dispatch(IEvent @event) => _state.WithExclusiveAccess(state => DispatchMessage(@event, state, TransportMessage.OutGoing.Create(@event)));

        public void Dispatch(IDomainCommand command) => _state.WithExclusiveAccess(state => DispatchMessage(command, state, TransportMessage.OutGoing.Create(command)));

        public async Task<TCommandResult> DispatchAsync<TCommandResult>(IDomainCommand<TCommandResult> command) => (TCommandResult)await DispatchMessageWithResponse(command);

        public async Task<TQueryResult> DispatchAsync<TQueryResult>(IQuery<TQueryResult> query) => (TQueryResult)await DispatchMessageWithResponse(query);

        public ClientConnection(IGlobalBusStateTracker globalBusStateTracker, IEndpoint endpoint, NetMQPoller poller, IUtcTimeTimeSource timeSource)
        {
            _state.WithExclusiveAccess(state =>
            {
                state.TimeSource = timeSource;

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
            public IGlobalBusStateTracker GlobalBusStateTracker;
            public readonly Dictionary<Guid, TaskCompletionSource<object>> ExpectedResponseTasks = new Dictionary<Guid, TaskCompletionSource<object>>();
            public readonly Dictionary<Guid, PendingDeliveryNotification> PendingDeliveryNotifications = new Dictionary<Guid, PendingDeliveryNotification>();
            public DealerSocket Socket;
            public NetMQPoller Poller;
            public readonly NetMQQueue<TransportMessage.OutGoing> DispatchQueue = new NetMQQueue<TransportMessage.OutGoing>();
            public IUtcTimeTimeSource TimeSource { get; set; }

            public void DispatchMessage(object sender, NetMQQueueEventArgs<TransportMessage.OutGoing> e)
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
                            MarkAsReceivedAsync(response);
#pragma warning restore 4014
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            });
        }

        async Task MarkAsReceivedAsync(TransportMessage.Response.Incoming response)
        {
            try
            {
                await Task.CompletedTask;
            }
            catch(Exception)
            {
                //todo: proper exception handling here.
                throw;
            }
        }

        Task<object> DispatchMessageWithResponse(IMessage message) => _state.WithExclusiveAccess(state =>
        {
            var taskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            var outGoingMessage = TransportMessage.OutGoing.Create(message);

            if(message.RequiresResponse())
            {
                state.ExpectedResponseTasks.Add(outGoingMessage.MessageId, taskCompletionSource);
            } else
            {
                taskCompletionSource.SetResult(null);
            }

            DispatchMessage(message, state, outGoingMessage);

            return taskCompletionSource.Task;
        });

        static void DispatchMessage(IMessage message, State @this, TransportMessage.OutGoing outGoingMessage)
        {
            if(outGoingMessage.IsExactlyOnceDeliveryMessage)
            {
                @this.PendingDeliveryNotifications.Add(outGoingMessage.MessageId, new PendingDeliveryNotification(outGoingMessage.MessageId, @this.TimeSource.UtcNow));
            }
            @this.GlobalBusStateTracker.SendingMessageOnTransport(outGoingMessage, message);
            @this.DispatchQueue.Enqueue(outGoingMessage);
        }

        class PendingDeliveryNotification
        {
            public PendingDeliveryNotification(Guid messageId, DateTime sentAt)
            {
                MessageId = messageId;
                SentAt = sentAt;
            }

            internal Guid MessageId { get; }
            internal DateTime SentAt { get; }
        }
    }
}
