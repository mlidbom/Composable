using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.System;
using Composable.System.Configuration;
using Composable.System.Data.SqlClient;
using Composable.System.Linq;
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
            internal readonly Dictionary<EndpointId, ClientConnection> EndpointConnections = new Dictionary<EndpointId, ClientConnection>();
            internal readonly HandlerStorage HandlerStorage = new HandlerStorage();
            internal readonly NetMQPoller Poller = new NetMQPoller();
            public IUtcTimeTimeSource TimeSource { get; set; }
            public MessageStorage MessageStorage { get; set; }
        }

        readonly IThreadShared<State> _state = ThreadShared<State>.WithTimeout(10.Seconds());

        public InterprocessTransport(IGlobalBusStateTracker globalBusStateTracker, IUtcTimeTimeSource timeSource, ISqlConnection connectionFactory) => _state.WithExclusiveAccess(@this =>
        {
            @this.MessageStorage = new MessageStorage(connectionFactory);
            @this.TimeSource = timeSource;
            @this.GlobalBusStateTracker = globalBusStateTracker;
        });

        public void Connect(IEndpoint endpoint) => _state.WithExclusiveAccess(@this =>
        {
            @this.EndpointConnections.Add(endpoint.Id, new ClientConnection(@this.GlobalBusStateTracker, endpoint, @this.Poller, @this.TimeSource, @this.MessageStorage));
            @this.HandlerStorage.AddRegistrations(endpoint.Id, endpoint.ServiceLocator.Resolve<IMessageHandlerRegistry>().HandledTypes().Select(TypeId.FromType).ToSet());
        });

        public void Stop() => _state.WithExclusiveAccess(state =>
        {
            Contract.State.Assert(state.Running);
            state.Running = false;
            state.Poller.Dispose();
            state.EndpointConnections.Values.ForEach(socket => socket.Dispose());
        });

        public void Start() => _state.WithExclusiveAccess(@this =>
        {
            Contract.State.Assert(!@this.Running);
            @this.Running = true;
            @this.MessageStorage.Start();
            @this.Poller.RunAsync();
        });

        public async Task DispatchAsync(ITransactionalExactlyOnceDeliveryEvent @event) => await _state.WithExclusiveAccess(async state =>
        {
            var eventHandlerEndpointIds = state.HandlerStorage.GetEventHandlerEndpoints(@event);

            var connections = eventHandlerEndpointIds.Select(endpointId => state.EndpointConnections[endpointId]).ToList();

            var saveMessageTask = state.MessageStorage.SaveMessageAsync(@event);
            var dispatchTasks = connections.Select(receiver => receiver.DispatchAsync(@event)).ToList();

            await saveMessageTask;
            await Task.WhenAll(dispatchTasks);
        });

        public async Task DispatchAsync(ITransactionalExactlyOnceDeliveryCommand command) => await _state.WithExclusiveAccess(async state =>
        {
            var endPointId = state.HandlerStorage.GetCommandHandlerEndpoint(command);
            var connection = state.EndpointConnections[endPointId];
            await Task.WhenAll(state.MessageStorage.SaveMessageAsync(command), connection.DispatchAsync(command));
        });

        public async Task<Task<TCommandResult>> DispatchAsyncAsync<TCommandResult>(ITransactionalExactlyOnceDeliveryCommand<TCommandResult> command) => await _state.WithExclusiveAccess(async state =>
        {
            var endPointId = state.HandlerStorage.GetCommandHandlerEndpoint(command);
            var connection = state.EndpointConnections[endPointId];

            var saveMessageTask = state.MessageStorage.SaveMessageAsync(command);
            var dispatchTask = connection.DispatchAsyncAsync(command);

            await saveMessageTask;
            return await dispatchTask;
        });

        public async Task<TQueryResult> DispatchAsync<TQueryResult>(IQuery<TQueryResult> query) => await _state.WithExclusiveAccess(async state =>
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
