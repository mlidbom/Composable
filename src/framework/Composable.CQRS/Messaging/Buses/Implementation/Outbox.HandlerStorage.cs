using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using Composable.Refactoring.Naming;

namespace Composable.Messaging.Buses.Implementation
{
    partial class Outbox
    {
        internal class HandlerStorage
        {
            bool _handlerHasBeenResolved;
            readonly ITypeMapper _typeMapper;
            readonly Dictionary<TypeId, InboxConnection> _commandHandlerMap = new Dictionary<TypeId, InboxConnection>();
            readonly Dictionary<TypeId, InboxConnection> _queryHandlerMap = new Dictionary<TypeId, InboxConnection>();
            readonly List<(TypeId EventType, InboxConnection Connection)> _eventHandlerRegistrations = new List<(TypeId EventType, InboxConnection Connection)>();

            public HandlerStorage(ITypeMapper typeMapper) => _typeMapper = typeMapper;

            internal void AddRegistrations(InboxConnection inboxConnection, ISet<TypeId> handledTypeIds)
            {
                var endpointId = inboxConnection.EndpointInformation.Id;
                Assert.Argument.NotNull(endpointId, handledTypeIds);
                Assert.State.Assert(!_handlerHasBeenResolved);//Our collections are safe for multithreaded read, but not for read/write. So ensure that no-one tries to change them after we start reading from them.

                foreach(var typeId in handledTypeIds)
                {
                    if(_typeMapper.TryGetType(typeId, out var type))
                    {
                        if(IsRemoteEvent(type))
                        {
                            _eventHandlerRegistrations.Add((_typeMapper.GetId(type), inboxConnection));
                        } else if(IsRemoteCommand(type))
                        {
                            _commandHandlerMap.Add(_typeMapper.GetId(type), inboxConnection);
                        } else if(IsRemoteQuery(type))
                        {
                            _queryHandlerMap.Add(_typeMapper.GetId(type), inboxConnection);
                        } else
                        {
                            throw new Exception($"Type {typeId} is neither a remote command, event or query.");
                        }
                    }
                }
            }

            //performance: Use static type and indexing trick to improve performance
            internal InboxConnection GetCommandHandlerEndpoint(MessageTypes.Remotable.ICommand command)
            {
                _handlerHasBeenResolved = true;
                var commandTypeId = _typeMapper.GetId(command.GetType());

                if(!_commandHandlerMap.TryGetValue(commandTypeId, out var endpointId))
                {
                    throw new NoHandlerForMessageTypeException(command.GetType());
                }

                return endpointId;
            }

            //performance: Use static type and indexing trick to improve performance
            internal InboxConnection GetQueryHandlerEndpoint(MessageTypes.IQuery query)
            {
                _handlerHasBeenResolved = true;
                var queryTypeId = _typeMapper.GetId(query.GetType());

                if(!_queryHandlerMap.TryGetValue(queryTypeId, out var endpointId))
                {
                    throw new NoHandlerForMessageTypeException(query.GetType());
                }

                return endpointId;
            }

            //performance: Use static type and indexing trick to improve performance
            internal IReadOnlyList<InboxConnection> GetEventHandlerEndpoints(MessageTypes.Remotable.ExactlyOnce.IEvent @event)
            {
                _handlerHasBeenResolved = true;
                var typedEventHandlerRegistrations = _eventHandlerRegistrations
                                                     .Where(me => _typeMapper.TryGetType(me.EventType, out var _))
                                                     .Select(me => new
                                                                   {
                                                                       EventType = _typeMapper.GetType(me.EventType),
                                                                       me.Connection
                                                                   }).ToList();

                return typedEventHandlerRegistrations
                       .Where(@this => @this.EventType.IsInstanceOfType(@event))
                       .Select(@this => @this.Connection)
                       .ToList();
            }

            static bool IsRemoteCommand(Type type) => typeof(MessageTypes.Remotable.ICommand).IsAssignableFrom(type);
            static bool IsRemoteEvent(Type type) => typeof(MessageTypes.Remotable.ExactlyOnce.IEvent).IsAssignableFrom(type);
            static bool IsRemoteQuery(Type type) => typeof(MessageTypes.Remotable.NonTransactional.IQuery).IsAssignableFrom(type);
        }
    }
}
