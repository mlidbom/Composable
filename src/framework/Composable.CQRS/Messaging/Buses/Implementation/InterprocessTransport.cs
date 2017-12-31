using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.System;
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
        }

        readonly IThreadShared<State> _state = ThreadShared<State>.WithTimeout(10.Seconds());

        public InterprocessTransport(IGlobalBusStateTracker globalBusStateTracker) => _state.WithExclusiveAccess(@this => @this.GlobalBusStateTracker = globalBusStateTracker);

        public void Connect(IEndpoint endpoint) => _state.WithExclusiveAccess(@this =>
        {
            @this.EndpointConnections.Add(endpoint.Id, new ClientConnection(@this.GlobalBusStateTracker, endpoint, @this.Poller));

            @this.HandlerStorage.AddRegistrations(endpoint.Id, endpoint.ServiceLocator.Resolve<IMessageHandlerRegistry>().HandledTypes());
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
            @this.Poller.RunAsync();
        });

        public Task DispatchAsync(IEvent @event) => _state.WithExclusiveAccess(state =>
        {
            var something = state.HandlerStorage.GetEventHandlerEndpoints(@event);
            var connections = something.Select(endpointId => state.EndpointConnections[endpointId]).ToList();
            connections.ForEach(receiver => receiver.Dispatch(@event));
            return Task.CompletedTask;
        });

        public Task DispatchAsync(IDomainCommand command) => _state.WithExclusiveAccess(state =>
        {
            var endPointId = state.HandlerStorage.GetCommandHandlerEndpoint(command);
            var connection = state.EndpointConnections[endPointId];
            connection.Dispatch(command);
            return Task.CompletedTask;
        });

        public async Task<TCommandResult> DispatchAsync<TCommandResult>(IDomainCommand<TCommandResult> command) => await _state.WithExclusiveAccess(async state =>
        {
            var endPointId = state.HandlerStorage.GetCommandHandlerEndpoint(command);
            var connection = state.EndpointConnections[endPointId];
            return await connection.DispatchAsync(command);
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
