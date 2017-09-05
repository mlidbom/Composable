// ReSharper disable ForCanBeConvertedToForeach this file needs these optimizations...

using System;
using System.Collections.Generic;
using System.Linq;

namespace Composable.Messaging.Events
{
    /// <summary>
    /// Calls all matching handlers in the order they were registered when an event is Dispatched.
    /// Handlers should be registered using the RegisterHandlers method in the constructor of the inheritor.
    /// </summary>
    class CallMatchingHandlersInRegistrationOrderEventDispatcher<TEvent> : IMutableEventDispatcher<TEvent>
        where TEvent : class
    {
        readonly List<KeyValuePair<Type, Action<object>>> _handlers = new List<KeyValuePair<Type, Action<object>>>();

        readonly List<Action<object>> _runBeforeHandlers = new List<Action<object>>();
        readonly List<Action<object>> _runAfterHandlers = new List<Action<object>>();
        readonly HashSet<Type> _ignoredEvents = new HashSet<Type>();
        int _totalHandlers;

        ///<summary>Registers handlers for the incoming events. All matching handlers will be called in the order they were registered.</summary>
        internal IEventHandlerRegistrar<TEvent> RegisterHandlers() => new RegistrationBuilder(this);

        public IEventHandlerRegistrar<TEvent> Register() => new RegistrationBuilder(this);

        class RegistrationBuilder : IEventHandlerRegistrar<TEvent>
        {
            readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TEvent> _owner;

            public RegistrationBuilder(CallMatchingHandlersInRegistrationOrderEventDispatcher<TEvent> owner) => _owner = owner;

            ///<summary>Registers a for any event that implements THandledEvent. All matching handlers will be called in the order they were registered.</summary>
            RegistrationBuilder For<THandledEvent>(Action<THandledEvent> handler) where THandledEvent : TEvent => ForGenericEvent(handler);

            ///<summary>Lets you register handlers for event interfaces that may be defined outside of the event hierarchy you specify with TEvent.
            /// Useful for listening to generic events such as IAggregateRootCreatedEvent or IAggregateRootDeletedEvent
            /// Be aware that the concrete event received MUST still actually inherit TEvent or there will be an InvalidCastException
            /// </summary>
            RegistrationBuilder ForGenericEvent<THandledEvent>(Action<THandledEvent> handler)
            {
                var eventType = typeof(THandledEvent);

                _owner._handlers.Add(new KeyValuePair<Type, Action<object>>(eventType, e => handler((THandledEvent)e)));
                _owner._totalHandlers++;
                return this;
            }

            RegistrationBuilder BeforeHandlers(Action<TEvent> runBeforeHandlers)
            {
                _owner._runBeforeHandlers.Add(e => runBeforeHandlers((TEvent)e));
                _owner._totalHandlers++;
                return this;
            }

            RegistrationBuilder AfterHandlers(Action<TEvent> runAfterHandlers)
            {
                _owner._runAfterHandlers.Add(e => runAfterHandlers((TEvent)e));
                return this;
            }

            RegistrationBuilder IgnoreUnhandled<T>()
            {
                _owner._ignoredEvents.Add(typeof(T));
                _owner._totalHandlers++;
                return this;
            }

            #region IEventHandlerRegistrar implementation.

            IEventHandlerRegistrar<TEvent> IEventHandlerRegistrar<TEvent>.ForGenericEvent<THandledEvent>(Action<THandledEvent> handler) => ForGenericEvent(handler);

            IEventHandlerRegistrar<TEvent> IEventHandlerRegistrar<TEvent>.BeforeHandlers<THandledEvent>(Action<THandledEvent> runBeforeHandlers)
            {
                return BeforeHandlers(e => runBeforeHandlers((THandledEvent)e));
            }

            IEventHandlerRegistrar<TEvent> IEventHandlerRegistrar<TEvent>.AfterHandlers<THandledEvent>(Action<THandledEvent> runAfterHandlers)
            {
                return AfterHandlers(e => runAfterHandlers((THandledEvent)e));
            }

            IEventHandlerRegistrar<TEvent> IEventHandlerRegistrar<TEvent>.IgnoreUnhandled<THandledEvent>() => IgnoreUnhandled<THandledEvent>();

            IEventHandlerRegistrar<TEvent> IEventHandlerRegistrar<TEvent>.For<THandledEvent>(Action<THandledEvent> handler) => For(handler);

            #endregion
        }

        Dictionary<Type, Action<object>[]> _typeToHandlerCache;
        int _cachedTotalHandlers;
        // ReSharper disable once StaticMemberInGenericType
        static readonly Action<object>[] NullHandlerList = new Action<object>[0];

        Action<object>[] GetHandlers(Type type)
        {
            if(_cachedTotalHandlers != _totalHandlers)
            {
                _cachedTotalHandlers = _totalHandlers;
                _typeToHandlerCache = new Dictionary<Type, Action<object>[]>();
            }

            Action<object>[] arrayResult;
            if(_typeToHandlerCache.TryGetValue(type, out arrayResult))
            {
                return arrayResult;
            }

            var result = new List<Action<object>>();
            var hasDispatchedEvent = false;

            result.AddRange(_runBeforeHandlers);

            for(var index = 0; index < _handlers.Count; index++)
            {
                if(_handlers[index].Key.IsAssignableFrom(type))
                {
                    hasDispatchedEvent = true;
                    result.Add(_handlers[index].Value);
                }
            }

            if(hasDispatchedEvent)
            {
                result.AddRange(_runAfterHandlers);
            } else
            {
                if(!_ignoredEvents.Any(ignoredEventType => ignoredEventType.IsAssignableFrom(type)))
                {
                    throw new EventUnhandledException(GetType(), type);
                }

                return _typeToHandlerCache[type] = NullHandlerList;
            }

            return _typeToHandlerCache[type] = result.ToArray();
        }

        public void Dispatch(TEvent evt)
        {
            if(_totalHandlers == 0)
            {
                throw new EventUnhandledException(GetType(), evt.GetType());
            }

            var handlers = GetHandlers(evt.GetType());
            for(var i = 0; i < handlers.Length; i++)
            {
                handlers[i](evt);
            }
        }
    }

    class EventUnhandledException : Exception
    {
        public EventUnhandledException(Type handlerType, Type eventType)
            : base($@"{handlerType} does not handle nor ignore incoming event {eventType}") {}
    }
}
