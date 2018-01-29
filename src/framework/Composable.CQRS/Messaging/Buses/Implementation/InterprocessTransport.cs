using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Refactoring.Naming;
using Composable.System;
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
            public EndpointId EndpointId;
            public Thread PollerThread;
        }

        readonly IThreadShared<State> _state = ThreadShared<State>.WithTimeout(10.Seconds());
        ITaskRunner _taskRunner;

        public InterprocessTransport(IGlobalBusStateTracker globalBusStateTracker, IUtcTimeTimeSource timeSource, ISqlConnection connectionFactory, ITypeMapper typeMapper, EndpointId endpointId, ITaskRunner taskRunner) => _state.WithExclusiveAccess(@this =>
        {
            _taskRunner = taskRunner;
            @this.EndpointId = endpointId;
            @this.HandlerStorage = new HandlerStorage(typeMapper);
            @this.TypeMapper = typeMapper;
            @this.MessageStorage = new MessageStorage(connectionFactory, typeMapper);
            @this.TimeSource = timeSource;
            @this.GlobalBusStateTracker = globalBusStateTracker;
        });

        public void Connect(IEndpoint endpoint) => _state.WithExclusiveAccess(@this =>
        {
            @this.EndpointConnections.Add(endpoint.Id, new ClientConnection(@this.GlobalBusStateTracker, endpoint, @this.Poller, @this.TimeSource, @this.MessageStorage, @this.TypeMapper, _taskRunner));
            @this.HandlerStorage.AddRegistrations(endpoint.Id, endpoint.ServiceLocator.Resolve<IMessageHandlerRegistry>().HandledRemoteMessageTypeIds());
        });

        public void Start() => _state.WithExclusiveAccess(state =>
        {
            Assert.State.Assert(!state.Running);
            state.Running = true;
            state.MessageStorage.Start();

            state.Poller = new NetMQPoller();
            state.PollerThread =  new Thread(() => state.Poller.Run()){Name = $"{nameof(InterprocessTransport)}_{nameof(state.PollerThread)}"};
            state.PollerThread.Start();
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

        public void DispatchIfTransactionCommits(BusApi.RemoteSupport.ExactlyOnce.IEvent exectlyOnceEvent) => _state.WithExclusiveAccess(state =>
        {
            var eventHandlerEndpointIds = state.HandlerStorage.GetEventHandlerEndpoints(exectlyOnceEvent);

            var connections = eventHandlerEndpointIds.Where(id => id != state.EndpointId)//We dispatch events to ourself synchronously so don't go doing it again here.
                                                     .Select(endpointId => state.EndpointConnections[endpointId])
                                                     .ToArray();

            if(connections.Any())//Don't waste time persisting if there are no receivers
            {
                state.MessageStorage.SaveMessage(exectlyOnceEvent, eventHandlerEndpointIds.ToArray());
                connections.ForEach(receiver => receiver.DispatchIfTransactionCommits(exectlyOnceEvent));
            }
        });

        public void DispatchIfTransactionCommits(BusApi.RemoteSupport.ExactlyOnce.ICommand exactlyOnceCommand) => _state.WithExclusiveAccess(state =>
        {
            var endPointId = state.HandlerStorage.GetCommandHandlerEndpoint(exactlyOnceCommand);
            var connection = state.EndpointConnections[endPointId];
            state.MessageStorage.SaveMessage(exactlyOnceCommand, endPointId);
            connection.DispatchIfTransactionCommits(exactlyOnceCommand);
        });

        public async Task DispatchAsync(BusApi.RemoteSupport.AtMostOnce.ICommand atMostOnceCommand)  => await _state.WithExclusiveAccess(async state =>
        {
            var endPointId = state.HandlerStorage.GetCommandHandlerEndpoint(atMostOnceCommand);
            var connection = state.EndpointConnections[endPointId];

            await connection.DispatchAsync(atMostOnceCommand).NoMarshalling();
        });

        public async Task<TCommandResult> DispatchAsync<TCommandResult>(BusApi.RemoteSupport.AtMostOnce.ICommand<TCommandResult> atMostOnceCommand) => await _state.WithExclusiveAccess(async state =>
        {
            var endPointId = state.HandlerStorage.GetCommandHandlerEndpoint(atMostOnceCommand);
            var connection = state.EndpointConnections[endPointId];

            return await connection.DispatchAsync(atMostOnceCommand);
        });

        public async Task<TQueryResult> DispatchAsync<TQueryResult>(BusApi.RemoteSupport.NonTransactional.IQuery<TQueryResult> query) => await _state.WithExclusiveAccess(async state =>
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
