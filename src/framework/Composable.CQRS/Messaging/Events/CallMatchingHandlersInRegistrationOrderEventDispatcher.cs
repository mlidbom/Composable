// ReSharper disable ForCanBeConvertedToForeach this file needs these optimizations...

using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Persistence.EventStore;
using Composable.SystemCE.ReflectionCE;

// ReSharper disable StaticMemberInGenericType

namespace Composable.Messaging.Events
{
    /// <summary>
    /// Calls all matching handlers in the order they were registered when an event is Dispatched.
    /// Handlers should be registered using the RegisterHandlers method in the constructor of the inheritor.
    /// </summary>
    public class CallMatchingHandlersInRegistrationOrderEventDispatcher<TEvent> : IMutableEventDispatcher<TEvent>
        where TEvent : class, MessageTypes.IEvent
    {
        abstract class RegisteredHandler
        {
            internal abstract Action<MessageTypes.IEvent>? TryCreateHandlerFor(Type eventType);
        }

        class RegisteredHandler<THandledEvent> : RegisteredHandler
            where THandledEvent : MessageTypes.IEvent
        {
            //Since handler has specified no preference for wrapper type the most generic of all will do and any wrapped event containing a matching event should be dispatched to this handler.
            readonly Action<THandledEvent> _handler;
            public RegisteredHandler(Action<THandledEvent> handler) => _handler = handler;
            internal override Action<MessageTypes.IEvent>? TryCreateHandlerFor(Type eventType)
            {
                if(typeof(THandledEvent).IsAssignableFrom(eventType))
                {
                    return @event => _handler((THandledEvent)@event);
                } else if(eventType.Is<MessageTypes.IWrapperEvent<THandledEvent>>())
                {
                    return @event => _handler(((MessageTypes.IWrapperEvent<THandledEvent>)@event).Event);
                } else
                {
                    return null;
                }
            }
        }

        class RegisteredWrappedHandler<THandledWrapperEvent> : RegisteredHandler
            where THandledWrapperEvent : MessageTypes.IWrapperEvent<MessageTypes.IEvent>
        {
            readonly Action<THandledWrapperEvent> _handler;

            public RegisteredWrappedHandler(Action<THandledWrapperEvent> handler) => _handler = handler;
            internal override Action<MessageTypes.IEvent>? TryCreateHandlerFor(Type eventType) =>
                typeof(THandledWrapperEvent).IsAssignableFrom(eventType)
                    ? (Action<MessageTypes.IEvent>?)(@event => _handler((THandledWrapperEvent)@event))
                    : null;
        }

        readonly List<RegisteredHandler> _handlers = new List<RegisteredHandler>();

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

            RegistrationBuilder ForWrapped<TWrapperEvent>(Action<TWrapperEvent> handler)
                where TWrapperEvent : MessageTypes.IWrapperEvent<TEvent>
            {
                _owner._handlers.Add(new RegisteredWrappedHandler<TWrapperEvent>(handler));
                _owner._totalHandlers++;
                return this;
            }

            ///<summary>Lets you register handlers for event interfaces that may be defined outside of the event hierarchy you specify with TEvent.
            /// Useful for listening to generic events such as IAggregateCreatedEvent or IAggregateDeletedEvent
            /// Be aware that the concrete event received MUST still actually inherit TEvent or there will be an InvalidCastException
            /// </summary>
            RegistrationBuilder ForGenericEvent<THandledEvent>(Action<THandledEvent> handler) where THandledEvent : MessageTypes.IEvent
            {
                _owner._handlers.Add(new RegisteredHandler<THandledEvent>(handler));
                _owner._totalHandlers++;
                return this;
            }

            RegistrationBuilder BeforeHandlers(Action<TEvent> runBeforeHandlers)
            {
                //Urgent: fix this. Use the registered handler classes above
                _owner._runBeforeHandlers.Add(e => runBeforeHandlers(((MessageTypes.IWrapperEvent<TEvent>)e).Event));
                _owner._totalHandlers++;
                return this;
            }

            RegistrationBuilder AfterHandlers(Action<TEvent> runAfterHandlers)
            {
                //Urgent: fix this
                _owner._runAfterHandlers.Add(e => runAfterHandlers(((MessageTypes.IWrapperEvent<TEvent>)e).Event));
                return this;
            }

            RegistrationBuilder IgnoreUnhandled<T>() where T : MessageTypes.IEvent
            {
                _owner._ignoredEvents.Add(typeof(T)); //Urgent: Remove?
                _owner._ignoredEvents.Add(typeof(MessageTypes.IWrapperEvent<T>)); //urgent: Is this correct?
                _owner._totalHandlers++;
                return this;
            }

            #region IEventHandlerRegistrar implementation.

            IEventHandlerRegistrar<TEvent> IEventHandlerRegistrar<TEvent>.ForGenericEvent<THandledEvent>(Action<THandledEvent> handler) => ForGenericEvent(handler);

            IEventHandlerRegistrar<TEvent> IEventHandlerRegistrar<TEvent>.BeforeHandlers<THandledEvent>(Action<THandledEvent> runBeforeHandlers) { return BeforeHandlers(e => runBeforeHandlers((THandledEvent)e)); }

            IEventHandlerRegistrar<TEvent> IEventHandlerRegistrar<TEvent>.AfterHandlers<THandledEvent>(Action<THandledEvent> runAfterHandlers) { return AfterHandlers(e => runAfterHandlers((THandledEvent)e)); }

            IEventHandlerRegistrar<TEvent> IEventHandlerRegistrar<TEvent>.IgnoreUnhandled<THandledEvent>() => IgnoreUnhandled<THandledEvent>();

            IEventHandlerRegistrar<TEvent> IEventHandlerRegistrar<TEvent>.For<THandledEvent>(Action<THandledEvent> handler) => For(handler);

            IEventHandlerRegistrar<TEvent> IEventHandlerRegistrar<TEvent>.ForWrapped<TWrapperEvent>(Action<TWrapperEvent> handler) => ForWrapped(handler);

            #endregion
        }

        Dictionary<Type, Action<MessageTypes.IEvent>[]> _typeToHandlerCache = new Dictionary<Type, Action<MessageTypes.IEvent>[]>();
        int _cachedTotalHandlers;
        // ReSharper disable once StaticMemberInGenericType
        static readonly Action<object>[] NullHandlerList = Array.Empty<Action<object>>();

        Action<MessageTypes.IEvent>[] GetHandlers(Type type, bool validateHandlerExists = true)
        {
            if(_cachedTotalHandlers != _totalHandlers)
            {
                _cachedTotalHandlers = _totalHandlers;
                _typeToHandlerCache = new Dictionary<Type, Action<MessageTypes.IEvent>[]>();
            }

            if(_typeToHandlerCache.TryGetValue(type, out var arrayResult))
            {
                return arrayResult;
            }

            var result = new List<Action<MessageTypes.IEvent>>();
            var hasFoundHandler = false;

            foreach(var registeredHandler in _handlers)
            {
                var handler = registeredHandler.TryCreateHandlerFor(type);
                if(handler != null)
                {
                    if(!hasFoundHandler)
                    {
                        result.AddRange(_runBeforeHandlers);
                        hasFoundHandler = true;
                    }

                    result.Add(handler);
                }
            }

            if(hasFoundHandler)
            {
                result.AddRange(_runAfterHandlers);
            } else
            {
                if(validateHandlerExists && !_ignoredEvents.Any(ignoredEventType => ignoredEventType.IsAssignableFrom(type)))
                {
                    throw new EventUnhandledException(GetType(), type);
                }

                return _typeToHandlerCache[type] = NullHandlerList;
            }

            return _typeToHandlerCache[type] = result.ToArray();
        }

        public void Dispatch(TEvent evt)
        {
            //Urgent: Wrapping here seems arguable at best.
            var wrapped = evt as MessageTypes.IWrapperEvent<MessageTypes.IEvent>
                       ?? MessageTypes.WrapperEvent.WrapEvent((MessageTypes.IEvent)evt);

            var handlers = GetHandlers(wrapped.GetType());
            for(var i = 0; i < handlers.Length; i++)
            {
                handlers[i](wrapped);
            }
        }

        public bool HandlesEvent<THandled>() => GetHandlers(typeof(THandled), validateHandlerExists: false).Any();
        public bool Handles(IAggregateEvent @event) => GetHandlers(@event.GetType(), validateHandlerExists: false).Any();
    }

    public class EventUnhandledException : Exception
    {
        public EventUnhandledException(Type handlerType, Type eventType)
            : base($@"{handlerType} does not handle nor ignore incoming event {eventType}") {}
    }
}
