using System;
using System.Collections.Generic;
using Composable.CQRS.EventSourcing;
using Composable.DomainEvents;
using System.Linq;
using NServiceBus;
using Composable.System;

namespace Composable.StuffThatDoesNotBelongHere
{
    public class MultiEventHandler<TImplementor, TEvent> : IHandleMessages<TEvent> 
        where TEvent : IAggregateRootEvent
        where TImplementor : MultiEventHandler<TImplementor, TEvent>
    {
        private readonly Dictionary<Type, Action<TEvent>> _handlers = new Dictionary<Type, Action<TEvent>>();
        private bool _shouldIgnoreUnHandled;

        private Action<TEvent> _runBeforeHandlers = _ => { };
        private Action<TEvent> _runAfterHandlers = _ => { };

        protected void IgnoreUnHandled()
        {
            _shouldIgnoreUnHandled = true;
        }

        protected RegistrationBuilder RegisterHandlers()
        {
            return new RegistrationBuilder(this);
        }

        public class RegistrationBuilder
        {
            private readonly MultiEventHandler<TImplementor, TEvent> _owner;

            public RegistrationBuilder(MultiEventHandler<TImplementor, TEvent> owner )
            {
                _owner = owner;
            }

            public RegistrationBuilder For<THandledEvent>(Action<THandledEvent> handler) where THandledEvent : TEvent
            {
                _owner._handlers.Add(typeof(THandledEvent), (@event) => handler((THandledEvent)@event));
                return this;
            }

            public RegistrationBuilder For(Type eventType, Action<TEvent> handler)
            {
                if(!typeof(TEvent).IsAssignableFrom(eventType))
                {
                    throw new Exception("{0} Does not implement {1}. \nYou cannot register a handler for an event type that does not implement the listened for event".FormatWith(eventType, typeof(TEvent)));
                }

                _owner._handlers.Add(eventType, handler);
                return this;
            }

            public RegistrationBuilder BeforeHandlers(Action<TEvent> runBeforeHandlers)
            {
                _owner._runBeforeHandlers = runBeforeHandlers;
                return this;
            }

            public RegistrationBuilder AfterHandlers(Action<TEvent> runAfterHandlers)
            {
                _owner._runAfterHandlers = runAfterHandlers;
                return this;
            }
        }

        public virtual void Handle(TEvent evt)
        {
            var handler = GetHandler(evt);
            if (handler != null)
            {
                _runBeforeHandlers(evt);
                handler(evt);
                _runAfterHandlers(evt);
            }
        }

        private Action<TEvent> GetHandler(TEvent evt) {
            var handlers = _handlers
                .Where(registration => registration.Key.IsAssignableFrom(evt.GetType()))
                .Select(registration => registration.Value);

            if(handlers.Count() > 1)
            {
                throw new AmbigousHandlerException(evt);
            }

            var handler = handlers.SingleOrDefault();

            if(handler == null)
            {
                if (_shouldIgnoreUnHandled)
                {
                    return handler;
                }
                throw new EventUnhandledException(this.GetType(), evt, typeof(TEvent));
            }
            return handler;
        }
    }

    public class AmbigousHandlerException : Exception
    {
        public AmbigousHandlerException(IDomainEvent evt) : base(evt.GetType().AssemblyQualifiedName)
        {
            
        }
    }

    public class EventUnhandledException : Exception
    {
        public EventUnhandledException(Type handlerType, IDomainEvent evt, Type listenedFor)
            : base(
  @"{0} does not handle nor ignore incoming event {1} matching listened for type {2}
It should either listen for more specific events or call IgnoreUnHandled".FormatWith(handlerType, evt.GetType(), listenedFor))
        {
            
        }
    }
}