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
            internal bool Running;
            public IGlobalBusStateTracker GlobalBusStateTracker;
            internal readonly Dictionary<EndpointId, IClientConnection> EndpointConnections = new Dictionary<EndpointId, IClientConnection>();
            internal HandlerStorage HandlerStorage;
            internal NetMQPoller Poller;
            public IUtcTimeTimeSource TimeSource { get; set; }
            public MessageStorage MessageStorage { get; set; }
            public ITypeMapper TypeMapper { get; set; }
            public IRemotableMessageSerializer Serializer { get; set; }
            public EndpointId EndpointId;
            public Thread PollerThread;
        }

        readonly IThreadShared<State> _state = ThreadShared<State>.Optimized();
        ITaskRunner _taskRunner;

        public InterprocessTransport(IGlobalBusStateTracker globalBusStateTracker, IUtcTimeTimeSource timeSource, ISqlConnection connectionFactory, ITypeMapper typeMapper, EndpointId endpointId, ITaskRunner taskRunner, IRemotableMessageSerializer serializer) => _state.WithExclusiveAccess(@this =>
        {
            _taskRunner = taskRunner;
            @this.EndpointId = endpointId;
            @this.Serializer = serializer;
            @this.HandlerStorage = new HandlerStorage(typeMapper);
            @this.TypeMapper = typeMapper;
            @this.MessageStorage = new MessageStorage(connectionFactory, typeMapper, serializer);
            @this.TimeSource = timeSource;
            @this.GlobalBusStateTracker = globalBusStateTracker;
        });

        //performance:tests: make async
        public async Task ConnectAsync(EndPointAddress remoteEndpoint) => await _state.WithExclusiveAccess(async @this =>
        {
            var clientConnection = await ClientConnection.CreateAsync(@this.GlobalBusStateTracker, remoteEndpoint, @this.Poller, @this.TimeSource, @this.MessageStorage, @this.TypeMapper, _taskRunner, @this.Serializer);
            @this.EndpointConnections.Add(clientConnection.EndPointinformation.Id, clientConnection);
            @this.HandlerStorage.AddRegistrations(clientConnection.EndPointinformation.Id, clientConnection.EndPointinformation.HandledMessageTypes);
        });

        public async Task StartAsync() => await _state.WithExclusiveAccess(async state =>
        {
            Assert.State.Assert(!state.Running);
            state.Running = true;
            var storageStartTask = state.MessageStorage.StartAsync();

            state.Poller = new NetMQPoller();
            state.PollerThread =  new Thread(() => state.Poller.Run()){Name = $"{nameof(InterprocessTransport)}_{nameof(state.PollerThread)}"};
            state.PollerThread.Start();

            await storageStartTask;
        });

        public void Stop() => _state.WithExclusiveAccess(state =>
        {
            Assert.State.Assert(state.Running);
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
                                               .Where(id => id != state.EndpointId)
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

        public async Task DispatchAsync(BusApi.Remotable.AtMostOnce.ICommand atMostOnceCommand)  => await _state.WithExclusiveAccess(async state =>
        {
            var endPointId = state.HandlerStorage.GetCommandHandlerEndpoint(atMostOnceCommand);
            var connection = state.EndpointConnections[endPointId];

            await connection.DispatchAsync(atMostOnceCommand).NoMarshalling();
        });

        public async Task<TCommandResult> DispatchAsync<TCommandResult>(BusApi.Remotable.AtMostOnce.ICommand<TCommandResult> atMostOnceCommand) => await _state.WithExclusiveAccess(async state =>
        {
            var endPointId = state.HandlerStorage.GetCommandHandlerEndpoint(atMostOnceCommand);
            var connection = state.EndpointConnections[endPointId];

            return await connection.DispatchAsync(atMostOnceCommand);
        });

        public async Task<TQueryResult> DispatchAsync<TQueryResult>(BusApi.Remotable.NonTransactional.IQuery<TQueryResult> query) => await _state.WithExclusiveAccess(async state =>
        {
            var endPointId = state.HandlerStorage.GetQueryHandlerEndpoint(query);
            var connection = state.EndpointConnections[endPointId];
            return await connection.DispatchAsync(query);
        });

        public void Dispose() => _state.WithExclusiveAccess(state =>
        {
            if(state.Running)
            {
                Stop();
            }
        });
    }
}
