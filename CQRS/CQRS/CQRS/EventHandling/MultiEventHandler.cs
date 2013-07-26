using System;
using System.Collections.Generic;
using System.Linq;
using Composable.CQRS.EventSourcing;
using Composable.System;
using Composable.System.Linq;
using NServiceBus;
using log4net;

namespace Composable.CQRS.EventHandling
{
    public class MultiEventHandler<TImplementor, TEvent> : IHandleMessages<TEvent>
        where TEvent : IAggregateRootEvent
        where TImplementor : MultiEventHandler<TImplementor, TEvent>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(MultiEventHandler<TImplementor, TEvent>));
        private readonly List<KeyValuePair<Type, Action<IAggregateRootEvent>>> _handlers = new List<KeyValuePair<Type, Action<IAggregateRootEvent>>>();

        private List<Action<IAggregateRootEvent>> _runBeforeHandlers = new List<Action<IAggregateRootEvent>>();
        private List<Action<IAggregateRootEvent>> _runAfterHandlers = new List<Action<IAggregateRootEvent>>();
        private HashSet<Type> _ignoredEvents = new HashSet<Type>();

        protected RegistrationBuilder RegisterHandlers()
        {
            return new RegistrationBuilder(this);
        }

        public class RegistrationBuilder
        {
            private readonly MultiEventHandler<TImplementor, TEvent> _owner;

            public RegistrationBuilder(MultiEventHandler<TImplementor, TEvent> owner)
            {
                _owner = owner;
            }

            public RegistrationBuilder For<THandledEvent>(Action<THandledEvent> handler) where THandledEvent : TEvent
            {
                var eventType = typeof(THandledEvent);

                if (!typeof(TEvent).IsAssignableFrom(eventType))
                {
                    throw new Exception(
                        "{0} Does not implement {1}. \nYou cannot register a handler for an event type that does not implement the listened for event"
                            .FormatWith(eventType, typeof(TEvent)));
                }

                return InternalUnsafeFor(handler);
            }

            internal RegistrationBuilder InternalUnsafeFor<THandledEvent>(Action<THandledEvent> handler) where THandledEvent : IAggregateRootEvent
            {
                var eventType = typeof(THandledEvent);
                if(_owner._handlers.Any(registration => registration.Key == eventType))
                {
                    throw new DuplicateHandlerRegistrationAttemptedException(eventType);
                }

                _owner._handlers.Add(new KeyValuePair<Type, Action<IAggregateRootEvent>>(eventType, e => handler((THandledEvent)e)));
                return this;
            }


            public RegistrationBuilder BeforeHandlers(Action<TEvent> runBeforeHandlers)
            {
                _owner._runBeforeHandlers.Add(e => runBeforeHandlers((TEvent)e));
                return this;
            }

            public RegistrationBuilder AfterHandlers(Action<TEvent> runAfterHandlers)
            {
                _owner._runAfterHandlers.Add(e => runAfterHandlers((TEvent)e));
                return this;
            }

            public RegistrationBuilder IgnoreUnhandled<T>()
            {
                _owner._ignoredEvents.Add(typeof(T));
                return this;
            }
        }

        public virtual void Handle(TEvent evt)
        {
            Log.DebugFormat("Handling event:{0}", evt);

            var handlers = GetHandler(evt).ToList();

            if(handlers.None())
            {
                if(_ignoredEvents.Any(ignoredEvent => ignoredEvent.IsAssignableFrom(evt.GetType())))
                {
                    return;
                }
                throw new EventUnhandledException(this.GetType(), evt);
            }

            foreach(var runBeforeHandler in _runBeforeHandlers)
            {
                runBeforeHandler(evt);
            }

            foreach(var handler in handlers)
            {
                handler(evt);
            }

            foreach(var runBeforeHandler in _runAfterHandlers)
            {
                runBeforeHandler(evt);
            }
        }

        private IEnumerable<Action<IAggregateRootEvent>> GetHandler(TEvent evt)
        {
            return _handlers
                .Where(registration => registration.Key.IsAssignableFrom(evt.GetType()))
                .Select(registration => registration.Value);
        }
    }

    public class DuplicateHandlerRegistrationAttemptedException : Exception
    {
        public DuplicateHandlerRegistrationAttemptedException(Type eventType) : base(eventType.AssemblyQualifiedName) {}
    }

    public class EventUnhandledException : Exception
    {
        public EventUnhandledException(Type handlerType, IAggregateRootEvent evt)
            : base(@"{0} does not handle nor ignore incoming event {1}".FormatWith(handlerType, evt.GetType())) {}
    }
}
