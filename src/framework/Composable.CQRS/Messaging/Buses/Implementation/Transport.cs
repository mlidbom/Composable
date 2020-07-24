using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.ThreadingCE;
using NetMQ;

namespace Composable.Messaging.Buses.Implementation
{
    class Transport : ITransport, IDisposable
    {
        readonly object _lock = new object();
        readonly IGlobalBusStateTracker _globalBusStateTracker;
        readonly ITypeMapper _typeMapper;

        bool _running;
        IReadOnlyDictionary<EndpointId, IInboxConnection> _inboxConnections = new Dictionary<EndpointId, IInboxConnection>();
        NetMQPoller? _poller;
        readonly IRemotableMessageSerializer _serializer;

        IReadOnlyDictionary<Type, IInboxConnection> _commandHandlerRoutes = new Dictionary<Type, IInboxConnection>();
        IReadOnlyDictionary<Type, IInboxConnection> _queryHandlerRoutes = new Dictionary<Type, IInboxConnection>();
        IReadOnlyList<(Type EventType, IInboxConnection Connection)> _eventSubscriberRoutes = new List<(Type EventType, IInboxConnection Connection)>();
        IReadOnlyDictionary<Type, IReadOnlyList<IInboxConnection>> _eventSubscriberRouteCache = new Dictionary<Type, IReadOnlyList<IInboxConnection>>();

        public Transport(IGlobalBusStateTracker globalBusStateTracker, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
        {
            _serializer = serializer;
            _globalBusStateTracker = globalBusStateTracker;
            _typeMapper = typeMapper;

            _poller = new NetMQPoller();
            _poller.RunAsync($"{nameof(Outbox)}_{nameof(NetMQPoller)}");
            _running = true;
        }

        public async Task ConnectAsync(EndPointAddress remoteEndpoint)
        {
            var clientConnection =  new Outbox.InboxConnection(_globalBusStateTracker, remoteEndpoint, _poller!, _typeMapper, _serializer);

            await clientConnection.Init().NoMarshalling();

            ThreadSafe.AddToCopyAndReplace(ref _inboxConnections, clientConnection.EndpointInformation.Id, clientConnection);

            AddRegistrations(clientConnection, clientConnection.EndpointInformation.HandledMessageTypes);
        }

        public void Stop()
        {
            Assert.State.Assert(_running, _poller != null, _poller.IsRunning);
            _running = false;
            _poller.StopAsync();
            _poller.Dispose();
            _inboxConnections.Values.ForEach(socket => socket.Dispose());
            _poller = null;
        }

        void AddRegistrations(IInboxConnection inboxConnection, ISet<TypeId> handledTypeIds)
        {
            var eventSubscribers = new List<(Type EventType, IInboxConnection Connection)>();
            var commandHandlerRoutes = new Dictionary<Type, IInboxConnection>();
            var queryHandlerRoutes = new Dictionary<Type, IInboxConnection>();
            foreach(var typeId in handledTypeIds)
            {
                if(_typeMapper.TryGetType(typeId, out var messageType))
                {
                    if(IsRemoteEvent(messageType))
                    {
                        eventSubscribers.Add((messageType, inboxConnection));
                    } else if(IsRemoteCommand(messageType))
                    {
                        commandHandlerRoutes.Add(messageType, inboxConnection);
                    } else if(IsRemoteQuery(messageType))
                    {
                        queryHandlerRoutes.Add(messageType, inboxConnection);
                    } else
                    {
                        throw new Exception($"Type {typeId} is neither a remote command, event or query.");
                    }
                }
            }

            lock(_lock)
            {
                if(eventSubscribers.Count > 0)
                {
                    ThreadSafe.AddRangeToCopyAndReplace(ref _eventSubscriberRoutes, eventSubscribers);
                    _eventSubscriberRouteCache = new Dictionary<Type, IReadOnlyList<IInboxConnection>>();
                }

                if(commandHandlerRoutes.Count > 0)
                    ThreadSafe.AddRangeToCopyAndReplace(ref _commandHandlerRoutes, commandHandlerRoutes);

                if(queryHandlerRoutes.Count > 0)
                    ThreadSafe.AddRangeToCopyAndReplace(ref _queryHandlerRoutes, queryHandlerRoutes);
            }
        }

        public async Task PostAsync(MessageTypes.Remotable.AtMostOnce.ICommand atMostOnceCommand)
        {
            var connection = ConnectionToHandlerFor(atMostOnceCommand);
            await connection.PostAsync(atMostOnceCommand).NoMarshalling();
        }

        public async Task<TCommandResult> PostAsync<TCommandResult>(MessageTypes.Remotable.AtMostOnce.ICommand<TCommandResult> atMostOnceCommand)
        {
            var connection = ConnectionToHandlerFor(atMostOnceCommand);
            return await connection.PostAsync(atMostOnceCommand).NoMarshalling();
        }

        public async Task<TQueryResult> GetAsync<TQueryResult>(MessageTypes.Remotable.NonTransactional.IQuery<TQueryResult> query)
        {
            var connection = ConnectionToHandlerFor(query);
            return await connection.GetAsync(query).NoMarshalling();
        }

        public IInboxConnection ConnectionToHandlerFor(MessageTypes.Remotable.ICommand command) =>
            _commandHandlerRoutes.TryGetValue(command.GetType(), out var connection)
                ? connection
                : throw new NoHandlerForMessageTypeException(command.GetType());

        IInboxConnection ConnectionToHandlerFor(MessageTypes.Remotable.NonTransactional.IQuery query) =>
            _queryHandlerRoutes.TryGetValue(query.GetType(), out var connection)
                ? connection
                : throw new NoHandlerForMessageTypeException(query.GetType());

        public IReadOnlyList<IInboxConnection> SubscriberConnectionsFor(MessageTypes.Remotable.ExactlyOnce.IEvent @event)
        {
            if(_eventSubscriberRouteCache.TryGetValue(@event.GetType(), out var connection)) return connection;

            var subscriberConnections = _eventSubscriberRoutes
                                       .Where(route => route.EventType.IsInstanceOfType(@event))
                                       .Select(route => route.Connection)
                                       .ToArray();


            ThreadSafe.AddToCopyAndReplace(ref _eventSubscriberRouteCache, @event.GetType(), subscriberConnections);
            return subscriberConnections;
        }

        static bool IsRemoteCommand(Type type) => typeof(MessageTypes.Remotable.ICommand).IsAssignableFrom(type);
        static bool IsRemoteEvent(Type type) => typeof(MessageTypes.Remotable.ExactlyOnce.IEvent).IsAssignableFrom(type);
        static bool IsRemoteQuery(Type type) => typeof(MessageTypes.Remotable.NonTransactional.IQuery).IsAssignableFrom(type);

        public void Dispose()
        {
            if(_running)
            {
                Stop();
            }
        }
    }
}
