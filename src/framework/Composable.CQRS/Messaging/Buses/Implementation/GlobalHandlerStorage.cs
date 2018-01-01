using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;

namespace Composable.Messaging.Buses.Implementation
{
    partial class InterprocessTransport
    {
        class HandlerStorage
        {
            readonly Dictionary<TypeId, Type> _idToTypeMap = new Dictionary<TypeId, Type>();
            readonly Dictionary<Type, TypeId> _typeToIdMap = new Dictionary<Type, TypeId>();
            readonly Dictionary<TypeId, EndpointId> _commandHandlerMap = new Dictionary<TypeId, EndpointId>();
            readonly Dictionary<TypeId, EndpointId> _queryHandlerMap = new Dictionary<TypeId, EndpointId>();

            readonly List<(TypeId EventType, EndpointId EndPointId)> _eventHandlerRegistrations = new List<(TypeId EventType, EndpointId EndPointId)>();

            internal void AddRegistrations(EndpointId endpointId, ISet<TypeId> handledTypeIds)
            {
                foreach(var typeId in handledTypeIds)
                {
                    var type = typeId.ToType();

                    if(IsEvent(type))
                    {
                        AddEventHandler(type, endpointId);
                    } else if(IsCommand(type))
                    {
                        AddCommandHandler(type, endpointId);
                    } else if(IsQuery(type))
                    {
                        AddQueryHandler(type, endpointId);
                    } else
                    {
                        Contract.Argument.Assert(false);
                    }
                }
            }

            void AddEventHandler(Type eventType, EndpointId endpointId)
            {
                Contract.Argument.NotNull(eventType, endpointId);
                var eventTypeId = TypeIdAttribute.Extract(eventType);

                _idToTypeMap.Add(eventTypeId, eventType);
                _typeToIdMap.Add(eventType, eventTypeId);
                _eventHandlerRegistrations.Add((eventTypeId, endpointId));
            }

            void AddCommandHandler(Type commandType, EndpointId endpointId)
            {
                Contract.Argument.NotNull(commandType, endpointId);
                var commandTypeId = TypeIdAttribute.Extract(commandType);
                _idToTypeMap.Add(commandTypeId, commandType);
                _typeToIdMap.Add(commandType, commandTypeId);
                _commandHandlerMap.Add(commandTypeId, endpointId);
            }

            void AddQueryHandler(Type queryType, EndpointId endpointId)
            {
                Contract.Argument.NotNull(queryType, endpointId);
                var queryTypeId = TypeIdAttribute.Extract(queryType);
                _idToTypeMap.Add(queryTypeId, queryType);
                _typeToIdMap.Add(queryType, queryTypeId);
                _queryHandlerMap.Add(queryTypeId, endpointId);
            }

            internal EndpointId GetCommandHandlerEndpoint(IDomainCommand command)
            {
                var commandTypeId = _typeToIdMap[command.GetType()];

                if(!_commandHandlerMap.TryGetValue(commandTypeId, out var endpointId))
                {
                    throw new NoHandlerForcommandTypeException(command.GetType());
                }

                return endpointId;
            }

            internal EndpointId GetQueryHandlerEndpoint(IQuery query)
            {
                var queryTypeId = _typeToIdMap[query.GetType()];
                return _queryHandlerMap[queryTypeId];
            }

            internal IReadOnlyList<EndpointId> GetEventHandlerEndpoints(IEvent @event)
            {
                var typedEventHandlerRegistrations = _eventHandlerRegistrations.Select(me => new
                                                                                             {
                                                                                                 EventType = _idToTypeMap[me.EventType],
                                                                                                 me.EndPointId
                                                                                             }).ToList();

                return typedEventHandlerRegistrations
                       .Where(@this => @this.EventType.IsInstanceOfType(@event))
                       .Select(@this => @this.EndPointId)
                       .ToList();
            }


            static bool IsCommand(Type type) => typeof(IDomainCommand).IsAssignableFrom(type);
            static bool IsEvent(Type type) => typeof(IEvent).IsAssignableFrom(type);
            static bool IsQuery(Type type) => typeof(IQuery).IsAssignableFrom(type);
        }
    }
}
