using System;
using System.Collections.Generic;
using Composable.DomainEvents;
using System.Linq;

namespace Composable.StuffThatDoesNotBelongHere
{
    public class EventPersister<TImplementor, TEvent> : IEventPersister<TEvent> where TEvent : IDomainEvent
    {
        private static readonly Dictionary<Type, Action<TEvent>> Handlers = new Dictionary<Type, Action<TEvent>>();
        private static bool ShouldIgnoreUnHandled;

        protected static void IgnoreUnHandled()
        {
            ShouldIgnoreUnHandled = true;
        }

        protected static RegistrationBuilder RegisterHandlers()
        {
            return new RegistrationBuilder();
        }

        public class RegistrationBuilder
        {
            public RegistrationBuilder For<THandledEvent>(Action<THandledEvent> handler) where THandledEvent : TEvent
            {
                Handlers.Add(typeof(THandledEvent), theEvent => handler((THandledEvent)theEvent));
                return this;
            }
        }

        public void Persist(TEvent evt)
        {
            var handlers = Handlers
                .Where(registration => registration.Key.IsAssignableFrom(evt.GetType()))
                .Select(registration => registration.Value);

            if(handlers.Count() > 1)
            {
                throw new AmbigousHandlerException(evt);
            }

            var handler = handlers.SingleOrDefault();

            if(handler != null)
            {
                handler(evt);
            }

            if(ShouldIgnoreUnHandled)
            {
                return;
            }

            throw new EventUnhandledException(evt);
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