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
    /// Calls all matching handlers in the order they were registered when an event is Dispatched.
    /// Handlers should be registered using the RegisterHandlers method in the constructor of the inheritor.
    /// </summary>
    public class CallMatchingHandlersInRegistrationOrderEventDispatcher<TEvent>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CallMatchingHandlersInRegistrationOrderEventDispatcher<TEvent>));
        private readonly List<KeyValuePair<Type, Action<object>>> _handlers = new List<KeyValuePair<Type, Action<object>>>();

        private readonly List<Action<object>> _runBeforeHandlers = new List<Action<object>>();
        private readonly List<Action<object>> _runAfterHandlers = new List<Action<object>>();
        private readonly HashSet<Type> _ignoredEvents = new HashSet<Type>();

        ///<summary>Registers handlers for the incoming events. All matching handlers will be called in the order they were registered.</summary>
        public RegistrationBuilder RegisterHandlers()
        {
            return new RegistrationBuilder(this);
        }

        public class RegistrationBuilder
        {
            private readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TEvent> _owner;

            public RegistrationBuilder(CallMatchingHandlersInRegistrationOrderEventDispatcher<TEvent> owner)
            {
                _owner = owner;
            }

            ///<summary>Registers a for any event that implements THandledEvent. All matching handlers will be called in the order they were registered.</summary>
            public RegistrationBuilder For<THandledEvent>(Action<THandledEvent> handler) where THandledEvent : TEvent
            {
                return ForGenericEvent(handler);
            }

            ///<summary>Lets you register handlers for event interfaces that may be defined outside of the event hierarchy you specify with TEvent.
            /// Useful for listening to generic events such as IAggregateRootCreatedEvent or IAggregateRootDeletedEvent
            /// Be aware that the concrete event received MUST still actually inherit TEvent or there will be an InvalidCastException
            /// </summary>
            public RegistrationBuilder ForGenericEvent<THandledEvent>(Action<THandledEvent> handler)
            {
                var eventType = typeof(THandledEvent);
                if(_owner._handlers.Any(registration => registration.Key == eventType))
                {
                    throw new DuplicateHandlerRegistrationAttemptedException(eventType);
                }

                _owner._handlers.Add(new KeyValuePair<Type, Action<object>>(eventType, e => handler((THandledEvent)e)));
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

        public void Dispatch(TEvent evt)
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

        private IEnumerable<Action<object>> GetHandler(TEvent evt)
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
        public EventUnhandledException(Type handlerType, object evt)
            : base(@"{0} does not handle nor ignore incoming event {1}".FormatWith(handlerType, evt.GetType())) {}
    }
}
