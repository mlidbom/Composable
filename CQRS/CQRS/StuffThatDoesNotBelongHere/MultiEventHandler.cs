using System;
using System.Collections.Generic;
using Composable.CQRS.EventSourcing;
using Composable.DomainEvents;
using System.Linq;
using NServiceBus;

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

        public void Handle(TEvent evt)
        {
            var handler = GetHandler(evt);
            _runBeforeHandlers(evt);
            handler(evt);    
            _runAfterHandlers(evt);
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
                throw new EventUnhandledException(evt);
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
        public EventUnhandledException(IDomainEvent evt):base(evt.GetType().AssemblyQualifiedName)
        {
            
        }
    }
}