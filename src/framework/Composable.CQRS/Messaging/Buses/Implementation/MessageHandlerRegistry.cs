using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Messaging.Events;
using Composable.Refactoring.Naming;
using Composable.System.Collections.Collections;
using Composable.System.Linq;
using Composable.System.Reflection;

namespace Composable.Messaging.Buses.Implementation
{
    //performance: Use static caching + indexing trick for storing and retrieving values throughout this class. QueryTypeIndexFor<TQuery>.Index. Etc
    class MessageHandlerRegistry : IMessageHandlerRegistrar, IMessageHandlerRegistry
    {
        readonly ITypeMapper _typeMapper;
        readonly Dictionary<Type, Action<object>> _commandHandlers = new Dictionary<Type, Action<object>>();
        readonly Dictionary<Type, List<Action<BusApi.IEvent>>> _eventHandlers = new Dictionary<Type, List<Action<BusApi.IEvent>>>();
        readonly Dictionary<Type, HandlerWithResultRegistration> _queryHandlers = new Dictionary<Type, HandlerWithResultRegistration>();
        readonly Dictionary<Type, HandlerWithResultRegistration> _commandHandlersReturningResults = new Dictionary<Type, HandlerWithResultRegistration>();
        readonly List<EventHandlerRegistration> _eventHandlerRegistrations = new List<EventHandlerRegistration>();

        readonly object _lock = new object();

        public MessageHandlerRegistry(ITypeMapper typeMapper) => _typeMapper = typeMapper;

        IMessageHandlerRegistrar IMessageHandlerRegistrar.ForEvent<TEvent>(Action<TEvent> handler)
        {
            MessageInspector.AssertValid<TEvent>();
            lock(_lock)
            {
                _eventHandlers.GetOrAdd(typeof(TEvent), () => new List<Action<BusApi.IEvent>>()).Add(@event => handler((TEvent)@event));
                _eventHandlerRegistrations.Add(new EventHandlerRegistration(typeof(TEvent), registrar => registrar.For(handler)));
                return this;
            }
        }

        IMessageHandlerRegistrar IMessageHandlerRegistrar.ForCommand<TCommand>(Action<TCommand> handler)
        {
            MessageInspector.AssertValid<TCommand>();

            if(typeof(TCommand).Implements(typeof(BusApi.ICommand<>)))
            {
                throw new Exception($"{typeof(TCommand)} expects a result. You must register a method that returns a result.");
            }

            lock(_lock)
            {
                _commandHandlers.Add(typeof(TCommand), command => handler((TCommand)command));
                return this;
            }
        }

        public IMessageHandlerRegistrar ForCommand<TCommand, TResult>(Func<TCommand, TResult> handler) where TCommand : BusApi.ICommand<TResult>
        {
            MessageInspector.AssertValid<TCommand>();
            lock (_lock)
            {
                _commandHandlersReturningResults.Add(typeof(TCommand), new CommandHandlerWithResultRegistration<TCommand, TResult>(handler));
                return this;
            }
        }

        IMessageHandlerRegistrar IMessageHandlerRegistrar.ForQuery<TQuery, TResult>(Func<TQuery, TResult> handler)
        {
            MessageInspector.AssertValid<TQuery>();
            lock(_lock)
            {
                _queryHandlers.Add(typeof(TQuery), new QueryHandlerRegistration<TQuery, TResult>(handler));
                return this;
            }
        }

        public IReadOnlyList<Type> GetTypesNeedingMappings()
        {
            var handledTypes = _commandHandlers.Keys
                                               .Concat(_commandHandlersReturningResults.Keys)
                                               .Concat(_queryHandlers.Keys)
                                               .Concat(_eventHandlerRegistrations.Select(reg => reg.Type))
                                               .Where(messageType => messageType.Implements<BusApi.Remotable.IMessage>())
                                               .ToSet();

            var remoteResultTypes = _commandHandlersReturningResults.Concat(_queryHandlers)
                                                                    .Where(handler => handler.Key.Implements<BusApi.Remotable.IMessage>())
                                                                    .Select(handler => handler.Value.ReturnValueType)
                                                                    .ToList();

            return handledTypes.Concat(remoteResultTypes).ToList();
        }

        Action<object> IMessageHandlerRegistry.GetCommandHandler(BusApi.ICommand message)
        {
            if(TryGetCommandHandler(message, out var handler))
            {
                return handler;
            }

            throw new NoHandlerException(message.GetType());
        }

        bool TryGetCommandHandler(BusApi.ICommand message, out Action<object> handler)
        {
            lock(_lock)
            {
                return _commandHandlers.TryGetValue(message.GetType(), out handler);
            }
        }

        public Func<BusApi.ICommand, object> GetCommandHandler(Type commandType)
        {
            if(_commandHandlers.TryGetValue(commandType, out var handler))
            {
                return command =>
                {
                    handler(command);
                    return null;
                };
            }

            return _commandHandlersReturningResults[commandType].HandlerMethod;
        }

        public Func<BusApi.IQuery, object> GetQueryHandler(Type queryType) => _queryHandlers[queryType].HandlerMethod;

        public IReadOnlyList<Action<BusApi.IEvent>> GetEventHandlers(Type eventType)
        {
            //performance: Use static caching trick.
            return _eventHandlers.Where(@this => @this.Key.IsAssignableFrom(eventType)).SelectMany(@this => @this.Value).ToList();
        }

        Func<BusApi.IQuery<TResult>, TResult> IMessageHandlerRegistry.GetQueryHandler<TResult>(BusApi.IQuery<TResult> query)
        {
            try
            {
                lock(_lock)
                {
                    var typeUnsafeQuery = _queryHandlers[query.GetType()].HandlerMethod;
                    return actualQuery => (TResult)typeUnsafeQuery(actualQuery);
                }
            }
            catch(KeyNotFoundException)
            {
                throw new NoHandlerException(query.GetType());
            }
        }

        public Func<BusApi.ICommand<TResult>, TResult> GetCommandHandler<TResult>(BusApi.ICommand<TResult> command)
        {
            try
            {
                lock (_lock)
                {
                    var typeUnsafeHandler = _commandHandlersReturningResults[command.GetType()].HandlerMethod;
                    return actualCommand => (TResult)typeUnsafeHandler(command);
                }
            }
            catch (KeyNotFoundException)
            {
                throw new NoHandlerException(command.GetType());
            }
        }


        IEventDispatcher<BusApi.IEvent> IMessageHandlerRegistry.CreateEventDispatcher()
        {
            var dispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<BusApi.IEvent>();
            var registrar = dispatcher.RegisterHandlers()
                                      .IgnoreUnhandled<BusApi.IEvent>();
            lock(_lock)
            {
                _eventHandlerRegistrations.ForEach(handlerRegistration => handlerRegistration.RegisterHandlerWithRegistrar(registrar));
            }

            return dispatcher;
        }

        public ISet<TypeId> HandledRemoteMessageTypeIds()
        {
            var handledTypes = _commandHandlers.Keys
                                               .Concat(_commandHandlersReturningResults.Keys)
                                               .Concat(_queryHandlers.Keys)
                                               .Concat(_eventHandlerRegistrations.Select(reg => reg.Type))
                                               .Where(messageType => messageType.Implements<BusApi.Remotable.IMessage>())
                                               .Where(messageType => !messageType.Implements<BusApi.Internal.IMessage>())
                                               .ToSet();


            var remoteResultTypes = _commandHandlersReturningResults
                                   .Where(handler => handler.Key.Implements<BusApi.Remotable.IMessage>())
                                   .Select(handler => handler.Value.ReturnValueType)
                                   .ToList();

            var typesNeedingMappings = handledTypes.Concat(remoteResultTypes);

            _typeMapper.AssertMappingsExistFor(typesNeedingMappings);

            return handledTypes.Select(_typeMapper.GetId)
                            .ToSet();
        }

        class EventHandlerRegistration
        {
            public Type Type { get; }
            public Action<IEventHandlerRegistrar<BusApi.IEvent>> RegisterHandlerWithRegistrar { get; }
            public EventHandlerRegistration(Type type, Action<IEventHandlerRegistrar<BusApi.IEvent>> registerHandlerWithRegistrar)
            {
                Type = type;
                RegisterHandlerWithRegistrar = registerHandlerWithRegistrar;
            }
        }

        abstract class HandlerWithResultRegistration
        {
            protected HandlerWithResultRegistration(Type returnValueType, Func<object, object> handlerMethod)
            {
                ReturnValueType = returnValueType;
                HandlerMethod = handlerMethod;
            }

            internal Type ReturnValueType { get; }
            internal Func<object, object> HandlerMethod{get;}
        }

        class CommandHandlerWithResultRegistration<TCommand, TResult> : HandlerWithResultRegistration
        {
            public CommandHandlerWithResultRegistration(Func<TCommand, TResult> handlerMethod) : base(typeof(TResult),
                                                                                                                            command =>
                                                                                                                            {
                                                                                                                                var result = handlerMethod((TCommand)command);
                                                                                                                                // ReSharper disable once CompareNonConstrainedGenericWithNull (null is never OK, but defaults might possibly be fine for structs.)
                                                                                                                                if(result == null)
                                                                                                                                {
                                                                                                                                    throw new Exception("You cannot return null from a command handler");
                                                                                                                                }

                                                                                                                                return result;
                                                                                                                            }) {}
        }

        class QueryHandlerRegistration<TQuery, TResult> : HandlerWithResultRegistration
        {
            public QueryHandlerRegistration(Func<TQuery, TResult> handlerMethod) : base(typeof(TResult),
                                                                                                      command =>
                                                                                                      {
                                                                                                          var result = handlerMethod((TQuery)command);
                                                                                                          // ReSharper disable once CompareNonConstrainedGenericWithNull (null is never OK, but defaults might possibly be fine for structs.)
                                                                                                          if(result == null)
                                                                                                          {
                                                                                                              throw new Exception("You cannot return null from a query handler");
                                                                                                          }

                                                                                                          return result;
                                                                                                      }) {}
        }
    }
}
