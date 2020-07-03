using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.GenericAbstractions.Time;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.System.Linq;
using Composable.System.Threading;
using Composable.System.Threading.ResourceAccess;
using NetMQ;

namespace Composable.Messaging.Buses.Implementation
{
    partial class Outbox : IOutbox, IDisposable
    {
        class State
        {
            public State(IGlobalBusStateTracker globalBusStateTracker, HandlerStorage handlerStorage, RealEndpointConfiguration configuration, IUtcTimeTimeSource timeSource, Outbox.IMessageStorage storage, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
            {
                GlobalBusStateTracker = globalBusStateTracker;
                HandlerStorage = handlerStorage;
                Configuration = configuration;
                TimeSource = timeSource;
                Storage = storage;
                TypeMapper = typeMapper;
                Serializer = serializer;
            }

            internal bool Running;
            public readonly IGlobalBusStateTracker GlobalBusStateTracker;
            internal readonly Dictionary<EndpointId, IInboxConnection> InboxConnections = new Dictionary<EndpointId, IInboxConnection>();
            internal readonly HandlerStorage HandlerStorage;
            internal NetMQPoller? Poller;
            public IUtcTimeTimeSource TimeSource { get; }
            public Outbox.IMessageStorage Storage { get; }
            public ITypeMapper TypeMapper { get; }
            public IRemotableMessageSerializer Serializer { get; }
            public readonly RealEndpointConfiguration Configuration;
            public Thread? PollerThread;
        }

        readonly IThreadShared<State> _state;
        readonly ITaskRunner _taskRunner;

        public Outbox(IGlobalBusStateTracker globalBusStateTracker, IUtcTimeTimeSource timeSource, Outbox.IMessageStorage messageStorage, ITypeMapper typeMapper, RealEndpointConfiguration configuration, ITaskRunner taskRunner, IRemotableMessageSerializer serializer)
        {
            _taskRunner = taskRunner;
            _state = new OptimizedThreadShared<State>(new State(
                                                          globalBusStateTracker,
                                                          new HandlerStorage(typeMapper),
                                                          configuration,
                                                          timeSource,
                                                          messageStorage,
                                                          typeMapper,
                                                          serializer));
        }

        public async Task ConnectAsync(EndPointAddress remoteEndpoint)
        {
            var clientConnection = _state.WithExclusiveAccess(@this => new InboxConnection(@this.GlobalBusStateTracker, remoteEndpoint, @this.Poller!, @this.TimeSource, @this.Storage, @this.TypeMapper, _taskRunner, @this.Serializer));

            await clientConnection.Init();

            _state.WithExclusiveAccess(@this =>
            {
                @this.InboxConnections.Add(clientConnection.EndpointInformation.Id, clientConnection);
                @this.HandlerStorage.AddRegistrations(clientConnection.EndpointInformation.Id, clientConnection.EndpointInformation.HandledMessageTypes);
            });
        }

        public async Task StartAsync()
        {
            Task storageStartTask = _state.WithExclusiveAccess(state =>
            {
                Assert.State.Assert(!state.Running);
                state.Running = true;

                storageStartTask = state.Configuration.IsPureClientEndpoint
                                       ? Task.CompletedTask
                                       : state.Storage.StartAsync();

                state.Poller = new NetMQPoller();
                state.PollerThread = new Thread(() => state.Poller.Run()) {Name = $"{nameof(Outbox)}_{nameof(state.PollerThread)}"};
                state.PollerThread.Start();
                return storageStartTask;
            });

            await storageStartTask;
        }

        public void Stop() => _state.WithExclusiveAccess(state =>
        {
            Assert.State.Assert(state.Running, state.PollerThread != null, state.Poller != null);
            state.Running = false;
            state.Poller.StopAsync();
            state.PollerThread.Join();
            state.Poller.Dispose();
            state.InboxConnections.Values.ForEach(socket => socket.Dispose());
            state.Poller = null;
        });

        public void DispatchIfTransactionCommits(MessageTypes.Remotable.ExactlyOnce.IEvent exactlyOnceEvent) => _state.WithExclusiveAccess(state =>
        {
            var eventHandlerEndpointIds = state.HandlerStorage.GetEventHandlerEndpoints(exactlyOnceEvent)
                                               .Where(id => id != state.Configuration.Id)
                                               .ToArray();//We dispatch events to ourself synchronously so don't go doing it again here.;

            if(eventHandlerEndpointIds.Length != 0)//Don't waste time if there are no receivers
            {
                var connections = eventHandlerEndpointIds.Select(endpointId => state.InboxConnections[endpointId])
                                                         .ToArray();
                state.Storage.SaveMessage(exactlyOnceEvent, eventHandlerEndpointIds);
                connections.ForEach(receiver => receiver.DispatchIfTransactionCommits(exactlyOnceEvent));
            }
        });

        public void DispatchIfTransactionCommits(MessageTypes.Remotable.ExactlyOnce.ICommand exactlyOnceCommand) => _state.WithExclusiveAccess(state =>
        {
            var endPointId = state.HandlerStorage.GetCommandHandlerEndpoint(exactlyOnceCommand);
            var connection = state.InboxConnections[endPointId];
            state.Storage.SaveMessage(exactlyOnceCommand, endPointId);
            connection.DispatchIfTransactionCommits(exactlyOnceCommand);
        });

        public async Task DispatchAsync(MessageTypes.Remotable.AtMostOnce.ICommand atMostOnceCommand)
        {
            IInboxConnection connection = _state.WithExclusiveAccess(state =>
            {
                var endPointId = state.HandlerStorage.GetCommandHandlerEndpoint(atMostOnceCommand);
                return state.InboxConnections[endPointId];
            });

            await connection.DispatchAsync(atMostOnceCommand).NoMarshalling();
        }

        public async Task<TCommandResult> DispatchAsync<TCommandResult>(MessageTypes.Remotable.AtMostOnce.ICommand<TCommandResult> atMostOnceCommand)
        {
            IInboxConnection connection = _state.WithExclusiveAccess(state =>
            {
                var endPointId = state.HandlerStorage.GetCommandHandlerEndpoint(atMostOnceCommand);
                return state.InboxConnections[endPointId];
            });

            return await connection.DispatchAsync(atMostOnceCommand);
        }

        public async Task<TQueryResult> DispatchAsync<TQueryResult>(MessageTypes.Remotable.NonTransactional.IQuery<TQueryResult> query)
        {
            var connection = _state.WithExclusiveAccess(state =>
            {
                var endPointId = state.HandlerStorage.GetQueryHandlerEndpoint(query);
                return state.InboxConnections[endPointId];
            });

            return await connection.DispatchAsync(query);
        }

        public void Dispose() => _state.WithExclusiveAccess(state =>
        {
            if(state.Running)
            {
                Stop();
            }
        });
    }
}
