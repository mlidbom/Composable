using System;
using System.Collections.Generic;
using System.Linq;
using Composable.CQRS.Command;
using Composable.CQRS.EventHandling;
using Composable.CQRS.EventSourcing;
using JetBrains.Annotations;

namespace Composable.ServiceBus
{
    public interface IMessageHandlerRegistrar
    {
        IMessageHandlerRegistrar ForEvent<TEvent>(Action<TEvent> handler) where TEvent : IEvent;
        IMessageHandlerRegistrar ForCommand<TCommand>(Action<TCommand> handler) where TCommand : ICommand;
    }

    [UsedImplicitly] public class InProcessServiceBus : IInProcessServiceBus, IMessageHandlerRegistrar
    {
        readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<IEvent> _eventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<IEvent>();

        readonly Dictionary<Type, Action<object>> _commandHandlers = new Dictionary<Type, Action<object>>();
        readonly List<EventHandlerRegistration> _eventHandlerRegistrations = new List<EventHandlerRegistration>();

        readonly  object _lock = new object();

        public void Publish(IEvent anEvent)
        {
            var dispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<IEvent>();
            var registrar = dispatcher.RegisterHandlers().IgnoreUnhandled<IEvent>();
            lock(_lock)
            {
                _eventHandlerRegistrations.ForEach(handlerRegistration => handlerRegistration.RegisterHandlerWithRegistrar(registrar));
            }            
            dispatcher.Dispatch((IEvent)anEvent);
            AfterDispatchingMessage((IMessage)anEvent);
        }

        public void Send(ICommand message)
        {
            Action<object> handler;
            lock(_lock)
            {
                try
                {
                    handler = _commandHandlers[message.GetType()];
                }
                catch(KeyNotFoundException e)
                {
                    throw new NoHandlerException(message.GetType());
                }
            }

            handler(message);
            AfterDispatchingMessage((IMessage)message);
        }

        public IMessageHandlerRegistrar ForEvent<TEvent>(Action<TEvent> handler) where TEvent : IEvent
        {
            _eventHandlerRegistrations.Add(new EventHandlerRegistration(typeof(TEvent), registrar => registrar.For(handler)));
            return this;
        }

        public IMessageHandlerRegistrar ForCommand<TCommand>(Action<TCommand> handler) where TCommand : ICommand
        {
            _commandHandlers.Add(typeof(TCommand), command => handler((TCommand)command));
            return this;
        }

        public bool Handles(object aMessage)
        {
            lock(_lock)
            {
                if(aMessage is IEvent)
                    return _eventHandlerRegistrations.Any(registration => registration.Type.IsInstanceOfType(aMessage));

                if(aMessage is ICommand)
                    return _commandHandlers.ContainsKey(aMessage.GetType());
            }
            throw new Exception($"Unhandled message type: {aMessage.GetType()}");
        }

        protected virtual void AfterDispatchingMessage(IMessage message)
        {
            
        }

        private class EventHandlerRegistration
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
