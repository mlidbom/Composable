using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.GenericAbstractions.Time;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.System.Data.SqlClient;
using Composable.System.Linq;
using Composable.System.Threading;
using Composable.System.Threading.ResourceAccess;
using NetMQ;

namespace Composable.Messaging.Buses.Implementation
{
    partial class InterprocessTransport : IInterprocessTransport, IDisposable
    {
        class State
        {
            public State(IGlobalBusStateTracker globalBusStateTracker, HandlerStorage handlerStorage, RealEndpointConfiguration configuration, IUtcTimeTimeSource timeSource, MessageStorage messageStorage, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
            {
                GlobalBusStateTracker = globalBusStateTracker;
                HandlerStorage = handlerStorage;
                Configuration = configuration;
                TimeSource = timeSource;
                MessageStorage = messageStorage;
                TypeMapper = typeMapper;
                Serializer = serializer;
            }

            internal bool Running;
            public readonly IGlobalBusStateTracker GlobalBusStateTracker;
            internal readonly Dictionary<EndpointId, IClientConnection> EndpointConnections = new Dictionary<EndpointId, IClientConnection>();
            internal readonly HandlerStorage HandlerStorage;
            internal NetMQPoller? Poller;
            public IUtcTimeTimeSource TimeSource { get; }
            public MessageStorage MessageStorage { get; }
            public ITypeMapper TypeMapper { get; }
            public IRemotableMessageSerializer Serializer { get; }
            public readonly RealEndpointConfiguration Configuration;
            public Thread? PollerThread;
        }

        readonly IThreadShared<State> _state;
        readonly ITaskRunner _taskRunner;

        public InterprocessTransport(IGlobalBusStateTracker globalBusStateTracker, IUtcTimeTimeSource timeSource, ISqlConnectionProvider connectionFactory, ITypeMapper typeMapper, RealEndpointConfiguration configuration, ITaskRunner taskRunner, IRemotableMessageSerializer serializer)
        {
            _taskRunner = taskRunner;
            _state = new OptimizedThreadShared<State>(new State(
                                                          globalBusStateTracker,
                                                          new HandlerStorage(typeMapper),
                                                          configuration,
                                                          timeSource,
                                                          new MessageStorage(connectionFactory, typeMapper, serializer),
                                                          typeMapper,
                                                          serializer));
        }

        public async Task ConnectAsync(EndPointAddress remoteEndpoint)
        {
            var clientConnection = _state.WithExclusiveAccess(@this => new ClientConnection(@this.GlobalBusStateTracker, remoteEndpoint, @this.Poller!, @this.TimeSource, @this.MessageStorage, @this.TypeMapper, _taskRunner, @this.Serializer));

            await clientConnection.Init(clientConnection);

            _state.WithExclusiveAccess(@this =>
            {
                @this.EndpointConnections.Add(clientConnection.EndPointinformation.Id, clientConnection);
                @this.HandlerStorage.AddRegistrations(clientConnection.EndPointinformation.Id, clientConnection.EndPointinformation.HandledMessageTypes);
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
                                       : state.MessageStorage.StartAsync();

                state.Poller = new NetMQPoller();
                state.PollerThread = new Thread(() => state.Poller.Run()) {Name = $"{nameof(InterprocessTransport)}_{nameof(state.PollerThread)}"};
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
            state.EndpointConnections.Values.ForEach(socket => socket.Dispose());
            state.Poller = null;
        });

        public void DispatchIfTransactionCommits(BusApi.Remotable.ExactlyOnce.IEvent exactlyOnceEvent) => _state.WithExclusiveAccess(state =>
        {
            var eventHandlerEndpointIds = state.HandlerStorage.GetEventHandlerEndpoints(exactlyOnceEvent)
                                               .Where(id => id != state.Configuration.Id)
                                               .ToArray();//We dispatch events to ourself synchronously so don't go doing it again here.;

            if(eventHandlerEndpointIds.Length != 0)//Don't waste time if there are no receivers
            {
                var connections = eventHandlerEndpointIds.Select(endpointId => state.EndpointConnections[endpointId])
                                                         .ToArray();
                state.MessageStorage.SaveMessage(exactlyOnceEvent, eventHandlerEndpointIds);
                connections.ForEach(receiver => receiver.DispatchIfTransactionCommits(exactlyOnceEvent));
            }
        });

        public void DispatchIfTransactionCommits(BusApi.Remotable.ExactlyOnce.ICommand exactlyOnceCommand) => _state.WithExclusiveAccess(state =>
        {
            var endPointId = state.HandlerStorage.GetCommandHandlerEndpoint(exactlyOnceCommand);
            var connection = state.EndpointConnections[endPointId];
            state.MessageStorage.SaveMessage(exactlyOnceCommand, endPointId);
            connection.DispatchIfTransactionCommits(exactlyOnceCommand);
        });

        public async Task DispatchAsync(BusApi.Remotable.AtMostOnce.ICommand atMostOnceCommand)
        {
            IClientConnection connection = _state.WithExclusiveAccess(state =>
            {
                var endPointId = state.HandlerStorage.GetCommandHandlerEndpoint(atMostOnceCommand);
                return state.EndpointConnections[endPointId];
            });

            await connection.DispatchAsync(atMostOnceCommand).NoMarshalling();
        }

        public async Task<TCommandResult> DispatchAsync<TCommandResult>(BusApi.Remotable.AtMostOnce.ICommand<TCommandResult> atMostOnceCommand)
        {
            IClientConnection connection = _state.WithExclusiveAccess(state =>
            {
                var endPointId = state.HandlerStorage.GetCommandHandlerEndpoint(atMostOnceCommand);
                return state.EndpointConnections[endPointId];
            });

            return await connection.DispatchAsync(atMostOnceCommand);
        }

        public async Task<TQueryResult> DispatchAsync<TQueryResult>(BusApi.Remotable.NonTransactional.IQuery<TQueryResult> query)
        {
            var connection = _state.WithExclusiveAccess(state =>
            {
                var endPointId = state.HandlerStorage.GetQueryHandlerEndpoint(query);
                return state.EndpointConnections[endPointId];
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
