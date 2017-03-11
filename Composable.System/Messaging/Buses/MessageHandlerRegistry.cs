using System;
using System.Collections.Generic;
using System.Linq;
using Composable.CQRS.EventSourcing;
using Composable.Messaging.Events;
using Composable.System.Reflection;

namespace Composable.Messaging.Buses
{
    class MessageHandlerRegistry : IMessageHandlerRegistrar, IMessageHandlerRegistry
    {
        readonly Dictionary<Type, Action<object>> _commandHandlers = new Dictionary<Type, Action<object>>();
        readonly Dictionary<Type, Func<object,object>> _queryHandlers = new Dictionary<Type, Func<object, object>>();
        readonly List<EventHandlerRegistration> _eventHandlerRegistrations = new List<EventHandlerRegistration>();

        readonly object _lock = new object();

        IMessageHandlerRegistrar IMessageHandlerRegistrar.ForEvent<TEvent>(Action<TEvent> handler)
        {
            lock(_lock)
            {
                _eventHandlerRegistrations.Add(new EventHandlerRegistration(typeof(TEvent), registrar => registrar.For(handler)));
                return this;
            }
        }

        IMessageHandlerRegistrar IMessageHandlerRegistrar.ForCommand<TCommand>(Action<TCommand> handler)
        {
            lock(_lock)
            {
                _commandHandlers.Add(typeof(TCommand), command => handler((TCommand)command));
                return this;
            }
        }

        IMessageHandlerRegistrar IMessageHandlerRegistrar.ForQuery<TQuery,TResult>(Func<TQuery, TResult> handler)
        {
            lock (_lock)
            {
                _queryHandlers.Add(typeof(TQuery), query => handler((TQuery)query));
                return this;
            }
        }

        Action<object> IMessageHandlerRegistry.GetCommandHandler(ICommand message)
        {
            try
            {
                lock(_lock)
                {
                    return _commandHandlers[message.GetType()];
                }
            }
            catch(KeyNotFoundException)
            {
                throw new NoHandlerException(message.GetType());
            }
        }

        Func<IQuery<TResult>, TResult> IMessageHandlerRegistry.GetQueryHandler<TResult>(IQuery<TResult> query)
        {
            try
            {
                lock (_lock)
                {
                    var typeUnsafeQuery = _queryHandlers[query.GetType()];
                    return actualQuery => (TResult)typeUnsafeQuery(actualQuery);
                }
            }
            catch (KeyNotFoundException)
            {
                throw new NoHandlerException(query.GetType());
            }
        }

        IEventDispatcher<IEvent> IMessageHandlerRegistry.CreateEventDispatcher()
        {
            var dispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<IEvent>();
            var registrar = dispatcher.RegisterHandlers()
                                      .IgnoreUnhandled<IEvent>();
            lock(_lock)
            {
                _eventHandlerRegistrations.ForEach(handlerRegistration => handlerRegistration.RegisterHandlerWithRegistrar(registrar));
            }

            return dispatcher;
        }

        bool IMessageHandlerRegistry.Handles(object aMessage)
        {
            lock(_lock)
            {
                if(aMessage is IEvent)
                    return _eventHandlerRegistrations.Any(registration => registration.Type.IsInstanceOfType(aMessage));

                if(aMessage is ICommand)
                    return _commandHandlers.ContainsKey(aMessage.GetType());

                if(aMessage.GetType().Implements(typeof(IQuery<>)))
                    return _queryHandlers.ContainsKey(aMessage.GetType());
            }

            throw new Exception($"Unhandled message type: {aMessage.GetType()}");
        }

        class EventHandlerRegistration
        {
            public Type Type { get; }
            public Action<IEventHandlerRegistrar<IEvent>> RegisterHandlerWithRegistrar { get; }
            public EventHandlerRegistration(Type type, Action<IEventHandlerRegistrar<IEvent>> registerHandlerWithRegistrar)
            {
                Type = type;
                RegisterHandlerWithRegistrar = registerHandlerWithRegistrar;
            }
        }
    }
}
