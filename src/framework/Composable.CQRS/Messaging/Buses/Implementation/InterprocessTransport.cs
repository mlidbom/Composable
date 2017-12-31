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
        class Implementation
        {
            internal bool Running;
            public IGlobalBusStateTracker GlobalBusStateTracker;
            internal readonly Dictionary<EndpointId, ClientConnection> EndpointConnections = new Dictionary<EndpointId, ClientConnection>();
            internal readonly HandlerStorage HandlerStorage = new HandlerStorage();
            internal readonly NetMQPoller Poller = new NetMQPoller();
        }

        readonly IGuardedResource<Implementation> _this = GuardedResource<Implementation>.WithTimeout(10.Seconds());

        public InterprocessTransport(IGlobalBusStateTracker globalBusStateTracker) => _this.Locked(@this => @this.GlobalBusStateTracker = globalBusStateTracker);

        public void Connect(IEndpoint endpoint) => _this.Locked(@this =>
        {
            var messageHandlers = endpoint.ServiceLocator.Resolve<IMessageHandlerRegistry>();

            var clientConnection = new ClientConnection(@this.GlobalBusStateTracker, endpoint, @this.Poller);

            @this.EndpointConnections.Add(endpoint.Id, clientConnection);

            @this.HandlerStorage.AddRegistrations(endpoint.Id, messageHandlers.HandledTypes());
        });

        public void Stop() => _this.Locked(@this =>
        {
            Contract.State.Assert(@this.Running);
            @this.Running = false;
            @this.Poller.Dispose();
            @this.EndpointConnections.Values.ForEach(socket => socket.Dispose());
        });

        public void Start() => _this.Locked(@this =>
        {
            Contract.State.Assert(!@this.Running);
            @this.Running = true;
            @this.Poller.RunAsync();
        });

        public Task DispatchAsync(IEvent @event) => _this.Locked(@this =>
        {
            var something = @this.HandlerStorage.GetEventHandlerEndpoints(@event);
            var connections = something.Select(endpointId => @this.EndpointConnections[endpointId]).ToList();
            connections.ForEach(receiver => receiver.Dispatch(@event));
            return Task.CompletedTask;
        });

        public Task DispatchAsync(IDomainCommand command) => _this.Locked(@this =>
        {
            var endPointId = @this.HandlerStorage.GetCommandHandler(command);
            var connection = @this.EndpointConnections[endPointId];
            connection.Dispatch(command);
            return Task.CompletedTask;
        });

        public async Task<TCommandResult> DispatchAsync<TCommandResult>(IDomainCommand<TCommandResult> command) => await _this.Locked(async @this =>
        {
            var endPointId = @this.HandlerStorage.GetCommandHandler(command);
            var connection = @this.EndpointConnections[endPointId];
            return await connection.DispatchAsync(command);
        });

        public async Task<TQueryResult> DispatchAsync<TQueryResult>(IQuery<TQueryResult> query) => await _this.Locked(async @this =>
        {
            var endPointId = @this.HandlerStorage.GetQueryHandler(query);
            var connection = @this.EndpointConnections[endPointId];
            return await connection.DispatchAsync(query);
        });

        public void Dispose() => _this.Locked(@this =>
        {
            if(@this.Running)
            {
                Stop();
            }
        });
    }
}
