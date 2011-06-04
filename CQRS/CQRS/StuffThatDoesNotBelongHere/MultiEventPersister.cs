using System;
using System.Collections.Generic;
using Composable.DomainEvents;
using System.Linq;

namespace Composable.StuffThatDoesNotBelongHere
{
    [Obsolete("This class is here to help us through the transition to getting rid if IEvent persinters in preference of IHandleEvents and the MultiEventHandler class")]
    public class MultiEventPersister<TImplementor, TEvent>
        where TEvent : IDomainEvent
        where TImplementor : MultiEventPersister<TImplementor, TEvent>
    {
        private static readonly Dictionary<Type, Action<TImplementor, TEvent>> Handlers = new Dictionary<Type, Action<TImplementor, TEvent>>();
        private static bool ShouldIgnoreUnHandled;

        private static Action<TImplementor, TEvent> RunBeforeHandlers = (_,__) => { };
        private static Action<TImplementor, TEvent> RunAfterHandlers = (_, __) => { };

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
            public RegistrationBuilder For<THandledEvent>(Action<TImplementor, THandledEvent> handler) where THandledEvent : TEvent
            {
                Handlers.Add(typeof(THandledEvent), (me, @event) => handler(me, (THandledEvent)@event));
                return this;
            }

            public RegistrationBuilder BeforeHandlers(Action<TImplementor, TEvent> runBeforeHandlers)
            {
                RunBeforeHandlers = runBeforeHandlers;
                return this;
            }

            public RegistrationBuilder AfterHandlers(Action<TImplementor, TEvent> runAfterHandlers)
            {
                RunAfterHandlers = runAfterHandlers;
                return this;
            }
        }

        public void Persist(TEvent evt)
        {
            var handler = GetHandler(evt);
            var implementor = (TImplementor)this;
            RunBeforeHandlers(implementor, evt);
            handler(implementor, evt);    
            RunAfterHandlers(implementor, evt);
        }

        private static Action<TImplementor, TEvent> GetHandler(TEvent evt) {
            var handlers = Handlers
                .Where(registration => registration.Key.IsAssignableFrom(evt.GetType()))
                .Select(registration => registration.Value);

            if(handlers.Count() > 1)
            {
                throw new AmbigousHandlerException(evt);
            }

            var handler = handlers.SingleOrDefault();

            if(handler == null)
            {
                if (ShouldIgnoreUnHandled)
                {
                    return handler;
                }
                throw new EventUnhandledException(evt);
            }
            return handler;
        }
    }
}