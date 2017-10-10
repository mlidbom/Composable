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
using NetMQ.Sockets;

namespace Composable.Messaging.Buses.Implementation
{
    class InterprocessTransport : IInterprocessTransport, IDisposable
    {
        class Implementation
        {
            // ReSharper disable InconsistentNaming
            public IGlobalBusStrateTracker _globalBusStrateTracker;

            public readonly Dictionary<Type, HashSet<ClientConnection>> _eventConnections = new Dictionary<Type, HashSet<ClientConnection>>();
            public readonly Dictionary<Type, ClientConnection> _commandConnections = new Dictionary<Type, ClientConnection>();
            public readonly Dictionary<Type, ClientConnection> _queryConnections = new Dictionary<Type, ClientConnection>();

            public bool _running;
            public readonly IList<ClientConnection> _clientConnections = new List<ClientConnection>();
            public NetMQPoller _poller;

            // ReSharper restore InconsistentNaming
        }

        readonly IGuardedResource<Implementation> _this = GuardedResource<Implementation>.WithTimeout(10.Seconds());

        public InterprocessTransport(IGlobalBusStrateTracker globalBusStrateTracker) => _this.Locked(@this => @this._globalBusStrateTracker = globalBusStrateTracker);

        public void Connect(IEndpoint endpoint) => _this.Locked(@this =>
        {
            var messageHandlers = endpoint.ServiceLocator.Resolve<IMessageHandlerRegistry>();

            var clientConnection = new ClientConnection(@this._globalBusStrateTracker, endpoint, @this._poller);

            var dealerSocket = new DealerSocket();
            //Should we screw up with the pipelining we prefer performance problems (memory usage) to lost messages or blocking
            dealerSocket.Options.SendHighWatermark = int.MaxValue;
            dealerSocket.Options.ReceiveHighWatermark = int.MaxValue;

            //We guarantee delivery upon restart in other ways. When we shut down, just do it.
            dealerSocket.Options.Linger = 0.Milliseconds();

            @this._clientConnections.Add(clientConnection);
            dealerSocket.Connect(endpoint.Address);
            @this._poller.Add(dealerSocket);

            foreach(var messageType in messageHandlers.HandledTypes())
            {
                if(IsEvent(messageType))
                {
                    @this._eventConnections.GetOrAdd(messageType, () => new HashSet<ClientConnection>()).Add(clientConnection);
                } else if(IsCommand(messageType))
                {
                    @this._commandConnections.Add(messageType, clientConnection);
                } else if(IsQuery(messageType))
                {
                    @this._queryConnections.Add(messageType, clientConnection);
                }
            }
        });


        static bool IsCommand(Type type) => typeof(ICommand).IsAssignableFrom(type);
        static bool IsEvent(Type type) => typeof(IEvent).IsAssignableFrom(type);
        static bool IsQuery(Type type) => typeof(IQuery).IsAssignableFrom(type);

        public void Stop()
        {
            var cheatingToAvoiddeadlockingWithReceiveResponseMethod = _this.Locked(@this =>
            {
                Contract.State.Assert(@this._running);
                @this._running = false;
                //@this._clientConnections.ForEach(socket => socket.Dispose());
                return @this;
            });

            cheatingToAvoiddeadlockingWithReceiveResponseMethod._poller.Dispose();
        }

        public void Start() => _this.Locked(@this =>
        {
            Contract.State.Assert(!@this._running);
            @this._running = true;
            @this._poller = new NetMQPoller();
            @this._poller.RunAsync();
        });

        public void Dispatch(IEvent @event)
        {
            var eventReceivers = _this.Locked(@this => @this._eventConnections[@event.GetType()].ToList());
            eventReceivers.ForEach(receiver => receiver.Dispatch(@event));
        }

        public void Dispatch(ICommand command) => _this.Locked(@this => @this._commandConnections[command.GetType()]).Dispatch(command);

        public Task<TCommandResult> Dispatch<TCommandResult>(ICommand<TCommandResult> command) where TCommandResult : IMessage
        {
            var commandHandlerConnection = _this.Locked(@this => @this._commandConnections[command.GetType()]);
            return commandHandlerConnection.Dispatch(command);
        }

        public Task<TQueryResult> Dispatch<TQueryResult>(IQuery<TQueryResult> query) where TQueryResult : IQueryResult
        {
            var commandHandlerConnection = _this.Locked(@this => @this._queryConnections[query.GetType()]);
            return commandHandlerConnection.Dispatch(query);
        }

        public void Dispose() => _this.Locked(@this =>
        {
            if(@this._running)
            {
                Stop();
            }
        });
    }
}
