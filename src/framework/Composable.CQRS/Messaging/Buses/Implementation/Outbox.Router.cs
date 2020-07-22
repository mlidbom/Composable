﻿using System;
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
            ImmutableDictionary<Type, IInboxConnection> _commandHandlerRoutes = ImmutableDictionary<Type, IInboxConnection>.Empty;
            ImmutableDictionary<Type, IInboxConnection> _queryHandlerRoutes = ImmutableDictionary<Type, IInboxConnection>.Empty;
            ImmutableList<(Type EventType, IInboxConnection Connection)> _eventSubscriberRoutes = ImmutableList<(Type EventType, IInboxConnection Connection)>.Empty;
            ImmutableDictionary<Type, ImmutableArray<IInboxConnection>> _eventSubscriberRouteCache = ImmutableDictionary<Type, ImmutableArray<IInboxConnection>>.Empty;

            public Router(ITypeMapper typeMapper) => _typeMapper = typeMapper;

            internal void AddRegistrations(IInboxConnection inboxConnection, ISet<TypeId> handledTypeIds)
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
                        _eventSubscriberRoutes = _eventSubscriberRoutes.AddRange(eventSubscribers);
                        _eventSubscriberRouteCache = ImmutableDictionary<Type, ImmutableArray<IInboxConnection>>.Empty;
                    }

                    _commandHandlerRoutes = _commandHandlerRoutes.AddRange(commandHandlerRoutes);
                    _queryHandlerRoutes = _queryHandlerRoutes.AddRange(queryHandlerRoutes);
                }
            }

            internal IInboxConnection ConnectionToHandlerFor(MessageTypes.Remotable.ICommand command) =>
                _commandHandlerRoutes.TryGetValue(command.GetType(), out var connection)
                    ? connection
                    : throw new NoHandlerForMessageTypeException(command.GetType());

            internal IInboxConnection ConnectionToHandlerFor(MessageTypes.IQuery query) =>
                _queryHandlerRoutes.TryGetValue(query.GetType(), out var connection)
                    ? connection
                    : throw new NoHandlerForMessageTypeException(query.GetType());

            internal IReadOnlyList<IInboxConnection> SubscriberConnectionsFor(MessageTypes.Remotable.ExactlyOnce.IEvent @event)
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