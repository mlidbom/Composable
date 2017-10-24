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
    class InterprocessTransport : IInterprocessTransport, IDisposable
    {
        class Implementation
        {
            public IGlobalBusStrateTracker GlobalBusStrateTracker;

            public readonly Dictionary<Type, HashSet<ClientConnection>> EventConnections = new Dictionary<Type, HashSet<ClientConnection>>();
            public readonly Dictionary<Type, ClientConnection> CommandConnections = new Dictionary<Type, ClientConnection>();
            public readonly Dictionary<Type, ClientConnection> QueryConnections = new Dictionary<Type, ClientConnection>();

            public bool Running;
            public readonly IList<ClientConnection> ClientConnections = new List<ClientConnection>();
            public readonly NetMQPoller Poller = new NetMQPoller();
        }

        readonly IGuardedResource<Implementation> _this = GuardedResource<Implementation>.WithTimeout(10.Seconds());

        public InterprocessTransport(IGlobalBusStrateTracker globalBusStrateTracker) => _this.Locked(@this => @this.GlobalBusStrateTracker = globalBusStrateTracker);

        public void Connect(IEndpoint endpoint) => _this.Locked(@this =>
        {
            var messageHandlers = endpoint.ServiceLocator.Resolve<IMessageHandlerRegistry>();

            var clientConnection = new ClientConnection(@this.GlobalBusStrateTracker, endpoint, @this.Poller);

            @this.ClientConnections.Add(clientConnection);

            foreach(var messageType in messageHandlers.HandledTypes())
            {
                if(IsEvent(messageType))
                {
                    @this.EventConnections.GetOrAdd(messageType, () => new HashSet<ClientConnection>()).Add(clientConnection);
                } else if(IsCommand(messageType))
                {
                    @this.CommandConnections.Add(messageType, clientConnection);
                } else if(IsQuery(messageType))
                {
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

        public void Dispatch(IEvent @event) => _this.Locked(@this =>
        {
            var eventReceivers = @this.EventConnections[@event.GetType()].ToList();
            eventReceivers.ForEach(receiver => receiver.Dispatch(@event));
        });

        public void Dispatch(IDomainCommand command) => _this.Locked(@this => @this.CommandConnections[command.GetType()]).Dispatch(command);

        public Task<TCommandResult> Dispatch<TCommandResult>(IDomainCommand<TCommandResult> command) => _this.Locked(@this =>
        {
            var commandHandlerConnection = @this.CommandConnections[command.GetType()];
            return commandHandlerConnection.Dispatch(command);
        });

        public Task<TQueryResult> Dispatch<TQueryResult>(IQuery<TQueryResult> query) => _this.Locked(@this =>
        {
            var commandHandlerConnection = @this.QueryConnections[query.GetType()];
            return commandHandlerConnection.Dispatch(query);
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
