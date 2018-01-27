using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using Composable.Messaging.Events;
using Composable.Refactoring.Naming;
using Composable.System.Collections.Collections;
using Composable.System.Linq;
using Composable.System.Reflection;

namespace Composable.Messaging.Buses
{
    class MessageHandlerRegistry : IMessageHandlerRegistrar, IMessageHandlerRegistry
    {
        readonly ITypeMapper _typeMapper;
        readonly Dictionary<Type, Action<object>> _commandHandlers = new Dictionary<Type, Action<object>>();
        readonly Dictionary<Type, List<Action<MessagingApi.Remote.ExactlyOnce.IEvent>>> _eventHandlers = new Dictionary<Type, List<Action<MessagingApi.Remote.ExactlyOnce.IEvent>>>();
        readonly Dictionary<Type, Func<object, object>> _queryHandlers = new Dictionary<Type, Func<object, object>>();
        readonly Dictionary<Type, Func<object, object>> _commandHandlersReturningResults = new Dictionary<Type, Func<object, object>>();
        readonly List<EventHandlerRegistration> _eventHandlerRegistrations = new List<EventHandlerRegistration>();

        readonly object _lock = new object();

        public MessageHandlerRegistry(ITypeMapper typeMapper) => _typeMapper = typeMapper;

        IMessageHandlerRegistrar IMessageHandlerRegistrar.ForEvent<TEvent>(Action<TEvent> handler)
        {
            MessageInspector.AssertValid<TEvent>();
            lock(_lock)
            {
                Assert.Argument.Assert(!(typeof(TEvent)).IsAssignableFrom(typeof(MessagingApi.Remote.ExactlyOnce.ICommand)), !(typeof(TEvent)).IsAssignableFrom(typeof(MessagingApi.IQuery)));
                _eventHandlers.GetOrAdd(typeof(TEvent), () => new List<Action<MessagingApi.Remote.ExactlyOnce.IEvent>>()).Add(@event => handler((TEvent)@event));
                _eventHandlerRegistrations.Add(new EventHandlerRegistration(typeof(TEvent), registrar => registrar.For(handler)));
                return this;
            }
        }

        IMessageHandlerRegistrar IMessageHandlerRegistrar.ForCommand<TCommand>(Action<TCommand> handler)
        {
            MessageInspector.AssertValid<TCommand>();

            if(typeof(TCommand).Implements(typeof(MessagingApi.Remote.ExactlyOnce.ICommand<>)))
            {
                throw new Exception($"{typeof(TCommand)} expects a result. You must register a method that returns a result.");
            }

            lock(_lock)
            {
                Assert.Argument.Assert(!(typeof(TCommand)).IsAssignableFrom(typeof(MessagingApi.Remote.ExactlyOnce.IEvent)), !(typeof(TCommand)).IsAssignableFrom(typeof(MessagingApi.IQuery)));
                _commandHandlers.Add(typeof(TCommand), command => handler((TCommand)command));
                return this;
            }
        }

        public IMessageHandlerRegistrar ForCommand<TCommand, TResult>(Func<TCommand, TResult> handler) where TCommand : MessagingApi.Remote.ExactlyOnce.ICommand<TResult>
        {
            MessageInspector.AssertValid<TCommand>();
            lock (_lock)
            {
                Assert.Argument.Assert(!(typeof(TCommand)).IsAssignableFrom(typeof(MessagingApi.Remote.ExactlyOnce.IEvent)), !(typeof(TCommand)).IsAssignableFrom(typeof(MessagingApi.IQuery)));
                _commandHandlersReturningResults.Add(typeof(TCommand), command =>
                {
                    var result = handler((TCommand)command);
                    // ReSharper disable once CompareNonConstrainedGenericWithNull (null is never OK, but defaults might possibly be fine for structs.)
                    if(result == null)
                    {
                        throw new Exception("You cannot return null from a command handler");
                    }
                    return result;
                });
                return this;
            }
        }

        IMessageHandlerRegistrar IMessageHandlerRegistrar.ForQuery<TQuery, TResult>(Func<TQuery, TResult> handler)
        {
            MessageInspector.AssertValid<TQuery>();
            lock(_lock)
            {
                Assert.Argument.Assert(!(typeof(TQuery)).IsAssignableFrom(typeof(MessagingApi.Remote.ExactlyOnce.IEvent)), !(typeof(TQuery)).IsAssignableFrom(typeof(MessagingApi.Remote.ExactlyOnce.ICommand)));
                _queryHandlers.Add(typeof(TQuery), query => handler((TQuery)query));
                return this;
            }
        }

        Action<object> IMessageHandlerRegistry.GetCommandHandler(MessagingApi.Remote.ExactlyOnce.ICommand message)
        {
            if(TryGetCommandHandler(message, out var handler))
            {
                return handler;
            }

            throw new NoHandlerException(message.GetType());
        }

        public bool TryGetCommandHandler(MessagingApi.Remote.ExactlyOnce.ICommand message, out Action<object> handler)
        {
            lock(_lock)
            {
                return _commandHandlers.TryGetValue(message.GetType(), out handler);
            }
        }

        public bool TryGetCommandHandlerWithResult(MessagingApi.Remote.ExactlyOnce.ICommand message, out Func<object, object> handler)
        {
            lock(_lock)
            {
                return _commandHandlersReturningResults.TryGetValue(message.GetType(), out handler);
            }
        }

        public Func<MessagingApi.Remote.ExactlyOnce.ICommand, object> GetCommandHandler(Type commandType)
        {
            if(_commandHandlers.TryGetValue(commandType, out var handler))
            {
                return command =>
                {
                    handler(command);
                    return null;
                };
            }

            return _commandHandlersReturningResults[commandType];
        }

        public Func<MessagingApi.IQuery, object> GetQueryHandler(Type queryType) => _queryHandlers[queryType];

        public IReadOnlyList<Action<MessagingApi.Remote.ExactlyOnce.IEvent>> GetEventHandlers(Type eventType)
        {
            return _eventHandlers.Where(@this => @this.Key.IsAssignableFrom(eventType)).SelectMany(@this => @this.Value).ToList();
        }

        Func<MessagingApi.IQuery<TResult>, TResult> IMessageHandlerRegistry.GetQueryHandler<TResult>(MessagingApi.IQuery<TResult> query)
        {
            try
            {
                lock(_lock)
                {
                    var typeUnsafeQuery = _queryHandlers[query.GetType()];
                    return actualQuery => (TResult)typeUnsafeQuery(actualQuery);
                }
            }
            catch(KeyNotFoundException)
            {
                throw new NoHandlerException(query.GetType());
            }
        }

        public Func<MessagingApi.Remote.ExactlyOnce.ICommand<TResult>, TResult> GetCommandHandler<TResult>(MessagingApi.Remote.ExactlyOnce.ICommand<TResult> command)
        {
            try
            {
                lock (_lock)
                {
                    var typeUnsafeHandler = _commandHandlersReturningResults[command.GetType()];
                    return actualCommand => (TResult)typeUnsafeHandler(command);
                }
            }
            catch (KeyNotFoundException)
            {
                throw new NoHandlerException(command.GetType());
            }
        }


        IEventDispatcher<MessagingApi.Remote.ExactlyOnce.IEvent> IMessageHandlerRegistry.CreateEventDispatcher()
        {
            var dispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<MessagingApi.Remote.ExactlyOnce.IEvent>();
            var registrar = dispatcher.RegisterHandlers()
                                      .IgnoreUnhandled<MessagingApi.Remote.ExactlyOnce.IEvent>();
            lock(_lock)
            {
                _eventHandlerRegistrations.ForEach(handlerRegistration => handlerRegistration.RegisterHandlerWithRegistrar(registrar));
            }

            return dispatcher;
        }

        public ISet<TypeId> HandledTypeIds()
        {
            var handledTypes = _commandHandlers.Keys
                                               .Concat(_commandHandlersReturningResults.Keys).Concat(_queryHandlers.Keys)
                                               .Concat(_eventHandlerRegistrations.Select(reg => reg.Type))
                                               .ToSet();

            _typeMapper.AssertMappingsExistFor(handledTypes);

            return handledTypes.Select(_typeMapper.GetId)
                            .ToSet();
        }

        internal class EventHandlerRegistration
        {
            public Type Type { get; }
            public Action<IEventHandlerRegistrar<MessagingApi.Remote.ExactlyOnce.IEvent>> RegisterHandlerWithRegistrar { get; }
            public EventHandlerRegistration(Type type, Action<IEventHandlerRegistrar<MessagingApi.Remote.ExactlyOnce.IEvent>> registerHandlerWithRegistrar)
            {
                Type = type;
                RegisterHandlerWithRegistrar = registerHandlerWithRegistrar;
            }
        }
    }
}
