using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.System;
using Composable.System.Collections.Collections;
using Composable.System.Linq;
using Composable.System.Threading.ResourceAccess;
using NetMQ;

namespace Composable.Messaging.Buses.Implementation
{
    partial class InterprocessTransport : IInterprocessTransport, IDisposable
    {
        class Implementation
        {
            public IGlobalBusStateTracker GlobalBusStateTracker;

            public readonly Dictionary<Type, HashSet<IClientConnection>> EventConnections = new Dictionary<Type, HashSet<IClientConnection>>();
            public readonly Dictionary<Type, IClientConnection> CommandConnections = new Dictionary<Type, IClientConnection>();
            public readonly Dictionary<Type, IClientConnection> QueryConnections = new Dictionary<Type, IClientConnection>();

            public bool Running;
            public readonly IList<ClientConnection> ClientConnections = new List<ClientConnection>();
            public HandlerStorage HandlerStorage = new HandlerStorage();
            public readonly NetMQPoller Poller = new NetMQPoller();
        }

        readonly IGuardedResource<Implementation> _this = GuardedResource<Implementation>.WithTimeout(10.Seconds());

        public InterprocessTransport(IGlobalBusStateTracker globalBusStateTracker) => _this.Locked(@this => @this.GlobalBusStateTracker = globalBusStateTracker);

        public void Connect(IEndpoint endpoint) => _this.Locked(@this =>
        {
            var messageHandlers = endpoint.ServiceLocator.Resolve<IMessageHandlerRegistry>();

            var clientConnection = new ClientConnection(@this.GlobalBusStateTracker, endpoint, @this.Poller);

            @this.ClientConnections.Add(clientConnection);

            foreach(var messageType in messageHandlers.HandledTypes())
            {
                if(IsEvent(messageType))
                {
                    @this.HandlerStorage.AddEventHandler(messageType);
                    @this.EventConnections.GetOrAdd(messageType, () => new HashSet<IClientConnection>()).Add(clientConnection);
                } else if(IsCommand(messageType))
                {
                    @this.HandlerStorage.AddCommandHandler(messageType);
                    @this.CommandConnections.Add(messageType, clientConnection);
                } else if(IsQuery(messageType))
                {
                    @this.HandlerStorage.AddQueryHandler(messageType);
                    @this.QueryConnections.Add(messageType, clientConnection);
                } else
                {
                    Contract.Argument.Assert(false);
                }
            }
        });

        public void Stop() => _this.Locked(@this =>
        {
            Contract.State.Assert(@this.Running);
            @this.Running = false;
            @this.Poller.Dispose();
            @this.ClientConnections.ForEach(socket => socket.Dispose());
        });

        public void Start() => _this.Locked(@this =>
        {
            Contract.State.Assert(!@this.Running);
            @this.Running = true;
            @this.Poller.RunAsync();
        });

        public Task DispatchAsync(IEvent @event) => _this.Locked(@this =>
        {
            var eventReceivers = @this.EventConnections.Where(me => me.Key.IsInstanceOfType(@event)).SelectMany(me => me.Value).Distinct().ToList();
            eventReceivers.ForEach(receiver => receiver.Dispatch(@event));
            return Task.CompletedTask;
        });

        public Task DispatchAsync(IDomainCommand command) => _this.Locked(@this =>
        {
            if(!@this.CommandConnections.TryGetValue(command.GetType(), out var connection))
            {
                throw new NoHandlerForcommandTypeException(command.GetType());
            }
            connection.Dispatch(command);
            return Task.CompletedTask;
        });

        public async Task<TCommandResult> DispatchAsync<TCommandResult>(IDomainCommand<TCommandResult> command) => await _this.Locked(async @this =>
        {
            if (!@this.CommandConnections.TryGetValue(command.GetType(), out var connection))
            {
                throw new NoHandlerForcommandTypeException(command.GetType());
            }
            return await connection.DispatchAsync(command);
        });

        public async Task<TQueryResult> DispatchAsync<TQueryResult>(IQuery<TQueryResult> query) => await _this.Locked(async @this =>
        {
            var commandHandlerConnection = @this.QueryConnections[query.GetType()];
            return await commandHandlerConnection.DispatchAsync(query);
        });

        public void Dispose() => _this.Locked(@this =>
        {
            if(@this.Running)
            {
                Stop();
            }
        });


        static bool IsCommand(Type type) => typeof(IDomainCommand).IsAssignableFrom(type);
        static bool IsEvent(Type type) => typeof(IEvent).IsAssignableFrom(type);
        static bool IsQuery(Type type) => typeof(IQuery).IsAssignableFrom(type);
    }
}
