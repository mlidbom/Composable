using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;
using Composable.Contracts;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.NetMQCE;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.System;
using Composable.System.Collections.Collections;
using Composable.System.Threading;
using Composable.System.Threading.ResourceAccess;
using Composable.SystemExtensions.TransactionsCE;
using NetMQ;
using NetMQ.Sockets;

namespace Composable.Messaging.Buses.Implementation
{
    class ClientConnection : IClientConnection
    {
        internal BusApi.Internal.EndpointInformation EndPointinformation { get; private set; }
        readonly ITypeMapper _typeMapper;
        readonly ITaskRunner _taskRunner;
        public void DispatchIfTransactionCommits(BusApi.Remotable.ExactlyOnce.IEvent @event) => Transaction.Current.OnCommittedSuccessfully(() => _state.WithExclusiveAccess(state => DispatchMessage(state, TransportMessage.OutGoing.Create(@event, state.TypeMapper, state.Serializer))));

        public void DispatchIfTransactionCommits(BusApi.Remotable.ExactlyOnce.ICommand command) => Transaction.Current.OnCommittedSuccessfully(() => _state.WithExclusiveAccess(state => DispatchMessage(state, TransportMessage.OutGoing.Create(command, state.TypeMapper, state.Serializer))));

        public async Task<TCommandResult> DispatchAsync<TCommandResult>(BusApi.Remotable.AtMostOnce.ICommand<TCommandResult> command)
        {
            var taskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            _state.WithExclusiveAccess(state =>
            {
                var outGoingMessage = TransportMessage.OutGoing.Create(command, state.TypeMapper, state.Serializer);

                state.ExpectedResponseTasks.Add(outGoingMessage.MessageId, taskCompletionSource);
                DispatchMessage(state, outGoingMessage);
            });

            return (TCommandResult)await taskCompletionSource.Task;
        }

        public async Task DispatchAsync(BusApi.Remotable.AtMostOnce.ICommand command)
        {
            var taskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            _state.WithExclusiveAccess(state =>
            {
                var outGoingMessage = TransportMessage.OutGoing.Create(command, state.TypeMapper, state.Serializer);

                state.ExpectedResponseTasks.Add(outGoingMessage.MessageId, taskCompletionSource);
                DispatchMessage(state, outGoingMessage);
            });

            await taskCompletionSource.Task;
        }

        public async Task<TQueryResult> DispatchAsync<TQueryResult>(BusApi.Remotable.NonTransactional.IQuery<TQueryResult> query)
        {
            var taskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            _state.WithExclusiveAccess(state =>
            {
                var outGoingMessage = TransportMessage.OutGoing.Create(query, state.TypeMapper, state.Serializer);

                state.ExpectedResponseTasks.Add(outGoingMessage.MessageId, taskCompletionSource);
                state.GlobalBusStateTracker.SendingMessageOnTransport(outGoingMessage);
                state.DispatchQueue.Enqueue(outGoingMessage);
            });

            return (TQueryResult)await taskCompletionSource.Task;
        }

        static void DispatchMessage(State @this, TransportMessage.OutGoing outGoingMessage)
        {
            if(outGoingMessage.IsExactlyOnceDeliveryMessage)
            {
                @this.PendingDeliveryNotifications.Add(outGoingMessage.MessageId, @this.TimeSource.UtcNow);
            }

            @this.GlobalBusStateTracker.SendingMessageOnTransport(outGoingMessage);
            @this.DispatchQueue.Enqueue(outGoingMessage);
        }

        internal async Task Init(ClientConnection clientConnection) { EndPointinformation = await clientConnection.DispatchAsync(new BusApi.Internal.EndpointInformationQuery()); }

        internal ClientConnection(IGlobalBusStateTracker globalBusStateTracker,
                                EndPointAddress serverEndpoint,
                                NetMQPoller poller,
                                IUtcTimeTimeSource timeSource,
                                InterprocessTransport.MessageStorage messageStorage,
                                ITypeMapper typeMapper,
                                ITaskRunner taskRunner,
                                IRemotableMessageSerializer serializer)
        {
            _typeMapper = typeMapper;
            _taskRunner = taskRunner;
            _state = new OptimizedThreadShared<State>(new State(typeMapper, timeSource, messageStorage, serializer, globalBusStateTracker));

            _state.WithExclusiveAccess(state =>
            {
                poller.Add(state.DispatchQueue);

                state.DispatchQueue.ReceiveReady += DispatchQueuedMessages;

                state.Socket = new DealerSocket();

                //Should we screw up with the pipelining we prefer performance problems (memory usage) to lost messages or blocking
                state.Socket.Options.SendHighWatermark = int.MaxValue;
                state.Socket.Options.ReceiveHighWatermark = int.MaxValue;

                //We guarantee delivery upon restart in other ways. When we shut down, just do it.
                state.Socket.Options.Linger = 0.Milliseconds();

                state.Socket.ReceiveReady += ReceiveResponse;

                state.Socket.Connect(serverEndpoint);
                poller.Add(state.Socket);
            });
        }

        void DispatchQueuedMessages(object sender,NetMQQueueEventArgs<TransportMessage.OutGoing> netMQQueueEventArgs) => _state.WithExclusiveAccess(state =>
        {
            while(netMQQueueEventArgs.Queue.TryDequeue(out var message, TimeSpan.Zero)) state.Socket.Send(message);
        });

        public void Dispose() => _state.WithExclusiveAccess(state =>
        {
            state.Socket.Dispose();
            state.DispatchQueue.Dispose();
        });

        class State
        {
            internal readonly IGlobalBusStateTracker GlobalBusStateTracker;
            internal readonly Dictionary<Guid, TaskCompletionSource<object?>> ExpectedResponseTasks = new Dictionary<Guid, TaskCompletionSource<object?>>();
            internal readonly Dictionary<Guid, DateTime> PendingDeliveryNotifications = new Dictionary<Guid, DateTime>();
            internal DealerSocket Socket;
            internal readonly NetMQQueue<TransportMessage.OutGoing> DispatchQueue = new NetMQQueue<TransportMessage.OutGoing>();
            internal IUtcTimeTimeSource TimeSource { get; set; }
            internal InterprocessTransport.MessageStorage MessageStorage { get; set; }
            public ITypeMapper TypeMapper { get; set; }
            public IRemotableMessageSerializer Serializer { get; set; }

            public State(ITypeMapper typeMapper, IUtcTimeTimeSource timeSource, InterprocessTransport.MessageStorage messageStorage, IRemotableMessageSerializer serializer, IGlobalBusStateTracker globalBusStateTracker)
            {
                TypeMapper = typeMapper;
                TimeSource = timeSource;
                MessageStorage = messageStorage;
                Serializer = serializer;
                GlobalBusStateTracker = globalBusStateTracker;
            }
        }

        readonly IThreadShared<State> _state;

        //Runs on poller thread so NO BLOCKING HERE!
        void ReceiveResponse(object sender, NetMQSocketEventArgs e)
        {
            var responseBatch = TransportMessage.Response.Incoming.ReceiveBatch(e.Socket, _typeMapper, 100);

            _state.WithExclusiveAccess(state =>
            {
                foreach(var response in responseBatch)
                {
                    switch(response.ResponseType)
                    {
                        case TransportMessage.Response.ResponseType.Success:
                        {
                            var successResponse = state.ExpectedResponseTasks.GetAndRemove(response.RespondingToMessageId);
                            _taskRunner.RunAndCrashProcessIfTaskThrows(() =>
                            {
                                try
                                {
                                    successResponse.SetResult(response.DeserializeResult(state.Serializer));
                                }
                                catch(Exception exception)
                                {
                                    successResponse.SetException(exception);
                                }
                            });
                        }
                            break;
                        case TransportMessage.Response.ResponseType.Failure:
                            var failureResponse = state.ExpectedResponseTasks.GetAndRemove(response.RespondingToMessageId);
                            failureResponse.SetException(new MessageDispatchingFailedException(response.Body));
                            break;
                        case TransportMessage.Response.ResponseType.Received:
                            Assert.Result.Assert(state.PendingDeliveryNotifications.Remove(response.RespondingToMessageId));
                            _taskRunner.RunAndCrashProcessIfTaskThrows(() => state.MessageStorage.MarkAsReceived(response, EndPointinformation.Id));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            });
        }
    }
}
