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
            readonly Dictionary<TypeId, EndpointId> _commandHandlerMap = new Dictionary<TypeId, EndpointId>();
            readonly Dictionary<TypeId, EndpointId> _queryHandlerMap = new Dictionary<TypeId, EndpointId>();
            readonly List<(TypeId EventType, EndpointId EndPointId)> _eventHandlerRegistrations = new List<(TypeId EventType, EndpointId EndPointId)>();

            public HandlerStorage(ITypeMapper typeMapper) => _typeMapper = typeMapper;

            internal void AddRegistrations(EndpointId endpointId, ISet<TypeId> handledTypeIds)
            {
                Assert.Argument.NotNull(endpointId, handledTypeIds);
                Assert.State.Assert(!_handlerHasBeenResolved);//Our collections are safe for multithreaded read, but not for read/write. So ensure that no-one tries to change them after we start reading from them.

                foreach(var typeId in handledTypeIds)
                {
                    if(_typeMapper.TryGetType(typeId, out var type))
                    {
                        if(IsRemoteEvent(type))
                        {
                            _eventHandlerRegistrations.Add((_typeMapper.GetId(type), endpointId));
                        } else if(IsRemoteCommand(type))
                        {
                            _commandHandlerMap.Add(_typeMapper.GetId(type), endpointId);
                        } else if(IsRemoteQuery(type))
                        {
                            _queryHandlerMap.Add(_typeMapper.GetId(type), endpointId);
                        } else
                        {
                            throw new Exception($"Type {typeId} is neither a remote command, event or query.");
                        }
                    }
                }
            }

            //performance: Use static type and indexing trick to improve performance
            internal EndpointId GetCommandHandlerEndpoint(MessageTypes.Remotable.ICommand command)
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
            internal EndpointId GetQueryHandlerEndpoint(MessageTypes.IQuery query)
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
            internal IReadOnlyList<EndpointId> GetEventHandlerEndpoints(MessageTypes.Remotable.ExactlyOnce.IEvent @event)
            {
                _handlerHasBeenResolved = true;
                var typedEventHandlerRegistrations = _eventHandlerRegistrations
                                                     .Where(me => _typeMapper.TryGetType(me.EventType, out var _))
                                                     .Select(me => new
                                                                   {
                                                                       EventType = _typeMapper.GetType(me.EventType),
                                                                       me.EndPointId
                                                                   }).ToList();

                return typedEventHandlerRegistrations
                       .Where(@this => @this.EventType.IsInstanceOfType(@event))
                       .Select(@this => @this.EndPointId)
                       .ToList();
            }

            static bool IsRemoteCommand(Type type) => typeof(MessageTypes.Remotable.ICommand).IsAssignableFrom(type);
            static bool IsRemoteEvent(Type type) => typeof(MessageTypes.Remotable.ExactlyOnce.IEvent).IsAssignableFrom(type);
            static bool IsRemoteQuery(Type type) => typeof(MessageTypes.Remotable.NonTransactional.IQuery).IsAssignableFrom(type);
        }
    }
}
