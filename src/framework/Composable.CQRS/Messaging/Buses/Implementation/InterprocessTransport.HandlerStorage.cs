using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using Composable.Refactoring.Naming;

namespace Composable.Messaging.Buses.Implementation
{
    partial class InterprocessTransport
    {
        class HandlerStorage
        {
            readonly ITypeMapper _typeMapper;
            readonly Dictionary<TypeId, EndpointId> _commandHandlerMap = new Dictionary<TypeId, EndpointId>();
            readonly Dictionary<TypeId, EndpointId> _queryHandlerMap = new Dictionary<TypeId, EndpointId>();
            readonly List<(TypeId EventType, EndpointId EndPointId)> _eventHandlerRegistrations = new List<(TypeId EventType, EndpointId EndPointId)>();

            public HandlerStorage(ITypeMapper typeMapper) => _typeMapper = typeMapper;

            internal void AddRegistrations(EndpointId endpointId, ISet<TypeId> handledTypeIds)
            {
                foreach(var typeId in handledTypeIds)
                {
                    if(_typeMapper.TryGetType(typeId, out var type))
                    {
                        if(IsRemoteEvent(type))
                        {
                            AddEventHandler(type, endpointId);
                        } else if(IsRemoteCommand(type))
                        {
                            AddCommandHandler(type, endpointId);
                        } else if(IsRemoteQuery(type))
                        {
                            AddQueryHandler(type, endpointId);
                        }
                    }
                }
            }

            void AddEventHandler(Type eventType, EndpointId endpointId)
            {
                Assert.Argument.NotNull(eventType, endpointId);
                var eventTypeId = _typeMapper.GetId(eventType);

                _eventHandlerRegistrations.Add((eventTypeId, endpointId));
            }

            void AddCommandHandler(Type commandType, EndpointId endpointId)
            {
                Assert.Argument.NotNull(commandType, endpointId);
                var commandTypeId = _typeMapper.GetId(commandType);
                _commandHandlerMap.Add(commandTypeId, endpointId);
            }

            void AddQueryHandler(Type queryType, EndpointId endpointId)
            {
                Assert.Argument.NotNull(queryType, endpointId);
                var queryTypeId = _typeMapper.GetId(queryType);
                _queryHandlerMap.Add(queryTypeId, endpointId);
            }

            internal EndpointId GetCommandHandlerEndpoint(BusApi.Remote.ExactlyOnce.ICommand command)
            {
                var commandTypeId = _typeMapper.GetId(command.GetType());

                if(!_commandHandlerMap.TryGetValue(commandTypeId, out var endpointId))
                {
                    throw new NoHandlerForMessageTypeException(command.GetType());
                }

                return endpointId;
            }

            internal EndpointId GetQueryHandlerEndpoint(BusApi.IQuery query)
            {
                var queryTypeId = _typeMapper.GetId(query.GetType());

                if(!_queryHandlerMap.TryGetValue(queryTypeId, out var endpointId))
                {
                    throw new NoHandlerForMessageTypeException(query.GetType());
                }

                return endpointId;
            }

            internal IReadOnlyList<EndpointId> GetEventHandlerEndpoints(BusApi.Remote.ExactlyOnce.IEvent @event)
            {
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

            static bool IsRemoteCommand(Type type) => typeof(BusApi.Remote.ExactlyOnce.ICommand).IsAssignableFrom(type);
            static bool IsRemoteEvent(Type type) => typeof(BusApi.Remote.ExactlyOnce.IEvent).IsAssignableFrom(type);
            static bool IsRemoteQuery(Type type) => typeof(BusApi.Remote.NonTransactional.IQuery).IsAssignableFrom(type);
        }
    }
}
