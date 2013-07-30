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
    /// <summary>
    /// Calls all matching handlers in the order they were registered when an event i received.
    /// </summary>
    public class CallsMatchingHandlersInRegistrationOrderEventHandler<TImplementor, TEvent> : IHandleMessages<TEvent>
        where TEvent : IAggregateRootEvent
        where TImplementor : CallsMatchingHandlersInRegistrationOrderEventHandler<TImplementor, TEvent>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CallsMatchingHandlersInRegistrationOrderEventHandler<TImplementor, TEvent>));
        private readonly List<KeyValuePair<Type, Action<IAggregateRootEvent>>> _handlers = new List<KeyValuePair<Type, Action<IAggregateRootEvent>>>();

        private readonly List<Action<IAggregateRootEvent>> _runBeforeHandlers = new List<Action<IAggregateRootEvent>>();
        private readonly List<Action<IAggregateRootEvent>> _runAfterHandlers = new List<Action<IAggregateRootEvent>>();
        private readonly HashSet<Type> _ignoredEvents = new HashSet<Type>();

        ///<summary>Registers handlers for the incoming events. All matching handlers will be called in the order they were registered.</summary>
        protected RegistrationBuilder RegisterHandlers()
        {
            return new RegistrationBuilder(this);
        }

        public class RegistrationBuilder
        {
            private readonly CallsMatchingHandlersInRegistrationOrderEventHandler<TImplementor, TEvent> _owner;

            public RegistrationBuilder(CallsMatchingHandlersInRegistrationOrderEventHandler<TImplementor, TEvent> owner)
            {
                _owner = owner;
            }

            ///<summary>Registers a for any event that implements THandledEvent. All matching handlers will be called in the order they were registered.</summary>
            public RegistrationBuilder For<THandledEvent>(Action<THandledEvent> handler) where THandledEvent : TEvent
            {
                return ForGenericEvent(handler);
            }

            ///<summary>Lets you register handlers for event interfaces that may be defined outside of the eventhierarchy of you aggregate.
            /// Useful for listening to generic events such as IAggregateRootCreatedEvent or IAggregateRootDeletedEvent
            /// </summary>
            public RegistrationBuilder ForGenericEvent<THandledEvent>(Action<THandledEvent> handler) where THandledEvent : IAggregateRootEvent
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
                if(_ignoredEvents.Any(ignoredEventType => ignoredEventType.IsInstanceOfType(evt)))
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
                .Where(registration => registration.Key.IsInstanceOfType(evt))
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
