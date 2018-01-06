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
            readonly Dictionary<TypeId, EndpointId> _commandHandlerMap = new Dictionary<TypeId, EndpointId>();
            readonly Dictionary<TypeId, EndpointId> _queryHandlerMap = new Dictionary<TypeId, EndpointId>();
            readonly List<(TypeId EventType, EndpointId EndPointId)> _eventHandlerRegistrations = new List<(TypeId EventType, EndpointId EndPointId)>();

            internal void AddRegistrations(EndpointId endpointId, ISet<TypeId> handledTypeIds)
            {
                foreach(var typeId in handledTypeIds)
                {
                    if(typeId.TryGetRuntimeType(out var type))
                    {
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
            }

            void AddEventHandler(Type eventType, EndpointId endpointId)
            {
                Contract.Argument.NotNull(eventType, endpointId);
                var eventTypeId = TypeId.FromType(eventType);

                _eventHandlerRegistrations.Add((eventTypeId, endpointId));
            }

            void AddCommandHandler(Type commandType, EndpointId endpointId)
            {
                Contract.Argument.NotNull(commandType, endpointId);
                var commandTypeId = TypeId.FromType(commandType);
                _commandHandlerMap.Add(commandTypeId, endpointId);
            }

            void AddQueryHandler(Type queryType, EndpointId endpointId)
            {
                Contract.Argument.NotNull(queryType, endpointId);
                var queryTypeId = TypeId.FromType(queryType);
                _queryHandlerMap.Add(queryTypeId, endpointId);
            }

            internal EndpointId GetCommandHandlerEndpoint(ITransactionalExactlyOnceDeliveryCommand command)
            {
                var commandTypeId = TypeId.FromType(command.GetType());

                if(!_commandHandlerMap.TryGetValue(commandTypeId, out var endpointId))
                {
                    throw new NoHandlerForMessageTypeException(command.GetType());
                }

                return endpointId;
            }

            internal EndpointId GetQueryHandlerEndpoint(IQuery query)
            {
                var queryTypeId = TypeId.FromType(query.GetType());

                if(!_queryHandlerMap.TryGetValue(queryTypeId, out var endpointId))
                {
                    throw new NoHandlerForMessageTypeException(query.GetType());
                }

                return endpointId;
            }

            internal IReadOnlyList<EndpointId> GetEventHandlerEndpoints(ITransactionalExactlyOnceDeliveryEvent @event)
            {
                var typedEventHandlerRegistrations = _eventHandlerRegistrations
                                                     .Where(me => me.EventType.TryGetRuntimeType(out var _))
                                                     .Select(me => new
                                                                   {
                                                                       EventType = me.EventType.GetRuntimeType(),
                                                                       me.EndPointId
                                                                   }).ToList();

                return typedEventHandlerRegistrations
                       .Where(@this => @this.EventType.IsInstanceOfType(@event))
                       .Select(@this => @this.EndPointId)
                       .ToList();
            }

            static bool IsCommand(Type type) => typeof(ITransactionalExactlyOnceDeliveryCommand).IsAssignableFrom(type);
            static bool IsEvent(Type type) => typeof(ITransactionalExactlyOnceDeliveryEvent).IsAssignableFrom(type);
            static bool IsQuery(Type type) => typeof(IQuery).IsAssignableFrom(type);
        }
    }
}
