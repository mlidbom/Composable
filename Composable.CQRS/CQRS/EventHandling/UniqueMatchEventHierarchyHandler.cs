using System;
using System.Collections.Generic;
using System.Linq;
using Composable.CQRS.EventSourcing;
using Composable.ServiceBus;
using Composable.StuffThatDoesNotBelongHere;
using Composable.System;
using log4net;
using NServiceBus;

namespace Composable.CQRS.EventHandling
{
    /// <summary>
    /// Dispatches handled events to a single matching handler only.
    /// If there is more than one matching handler an AmbigousHandlerException is thrown. 
    /// Use this base class when you are interested in exactly what has happened.
    /// </summary>
    public abstract class UniqueMatchEventHierarchyHandler<TImplementor, TEvent> : IHandleReplayedAndPublishedEvents<TEvent>
        where TImplementor : UniqueMatchEventHierarchyHandler<TImplementor, TEvent>
        where TEvent : IAggregateRootEvent
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(UniqueMatchEventHierarchyHandler<TImplementor, TEvent>));
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
            private readonly UniqueMatchEventHierarchyHandler<TImplementor, TEvent> _owner;

            public RegistrationBuilder(UniqueMatchEventHierarchyHandler<TImplementor, TEvent> owner)
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
                    throw new Exception(
                        "{0} Does not implement {1}. \nYou cannot register a handler for an event type that does not implement the listened for event".FormatWith(eventType,
                            typeof(TEvent)));
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
            if(handler != null)
            {
                Log.DebugFormat("Handling event:{0}", evt);
                _runBeforeHandlers(evt);
                handler(evt);
                _runAfterHandlers(evt);
            }
            else
            {
                Log.DebugFormat("Ignored event: {0}", evt);
            }
        }

        private Action<TEvent> GetHandler(TEvent evt)
        {
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
                if(_shouldIgnoreUnHandled)
                {
                    return handler;
                }
                throw new StuffThatDoesNotBelongHere.EventUnhandledException(this.GetType(), evt, typeof(TEvent));
            }
            return handler;
        }
    }
}