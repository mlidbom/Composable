using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Composable.Refactoring.Naming;

namespace Composable.Messaging.Buses.Implementation
{
    partial class Outbox
    {
        internal class Router
        {
            readonly object _lock = new object();
            readonly ITypeMapper _typeMapper;
            ImmutableDictionary<Type, InboxConnection> _commandHandlerRoutes = ImmutableDictionary<Type, InboxConnection>.Empty;
            ImmutableDictionary<Type, InboxConnection> _queryHandlerRoutes = ImmutableDictionary<Type, InboxConnection>.Empty;
            ImmutableList<(Type EventType, InboxConnection Connection)> _eventSubscriberRoutes = ImmutableList<(Type EventType, InboxConnection Connection)>.Empty;
            ImmutableDictionary<Type, ImmutableArray<InboxConnection>> _eventSubscriberRouteCache = ImmutableDictionary<Type, ImmutableArray<InboxConnection>>.Empty;

            public Router(ITypeMapper typeMapper) => _typeMapper = typeMapper;

            internal void AddRegistrations(InboxConnection inboxConnection, ISet<TypeId> handledTypeIds)
            {
                var eventSubscribers = new List<(Type EventType, InboxConnection Connection)>();
                var commandHandlerRoutes = new Dictionary<Type, InboxConnection>();
                var queryHandlerRoutes = new Dictionary<Type, InboxConnection>();
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
                        _eventSubscriberRoutes = _eventSubscriberRoutes.AddRange(eventSubscribers);
                        _eventSubscriberRouteCache = ImmutableDictionary<Type, ImmutableArray<InboxConnection>>.Empty;
                    }

                    _commandHandlerRoutes = _commandHandlerRoutes.AddRange(commandHandlerRoutes);
                    _queryHandlerRoutes = _queryHandlerRoutes.AddRange(queryHandlerRoutes);
                }
            }

            internal InboxConnection ConnectionToHandlerFor(MessageTypes.Remotable.ICommand command) =>
                _commandHandlerRoutes.TryGetValue(command.GetType(), out var connection)
                    ? connection
                    : throw new NoHandlerForMessageTypeException(command.GetType());

            internal InboxConnection ConnectionToHandlerFor(MessageTypes.IQuery query) =>
                _queryHandlerRoutes.TryGetValue(query.GetType(), out var connection)
                    ? connection
                    : throw new NoHandlerForMessageTypeException(query.GetType());

            internal IReadOnlyList<InboxConnection> SubscriberConnectionsFor(MessageTypes.Remotable.ExactlyOnce.IEvent @event)
            {
                if(_eventSubscriberRouteCache.TryGetValue(@event.GetType(), out var connection)) return connection;

                var subscriberConnections = _eventSubscriberRoutes
                      .Where(route => route.EventType.IsInstanceOfType(@event))
                      .Select(route => route.Connection)
                      .ToImmutableArray();

                lock(_lock)
                {
                    _eventSubscriberRouteCache = _eventSubscriberRouteCache.Add(@event.GetType(), subscriberConnections);
                    return subscriberConnections;
                }
            }

            static bool IsRemoteCommand(Type type) => typeof(MessageTypes.Remotable.ICommand).IsAssignableFrom(type);
            static bool IsRemoteEvent(Type type) => typeof(MessageTypes.Remotable.ExactlyOnce.IEvent).IsAssignableFrom(type);
            static bool IsRemoteQuery(Type type) => typeof(MessageTypes.Remotable.NonTransactional.IQuery).IsAssignableFrom(type);
        }
    }
}
