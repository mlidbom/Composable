// ReSharper disable ForCanBeConvertedToForeach this file needs these optimizations...

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Composable.Persistence.EventStore;
using Composable.SystemCE;
using Composable.SystemCE.ReflectionCE;
using Composable.SystemCE.ReflectionCE.EmitCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;

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
            internal abstract Action<object>? TryCreateHandlerFor(Type eventType);
        }

        class RegisteredHandler<THandledEvent> : RegisteredHandler
            where THandledEvent : MessageTypes.IEvent
        {
            //Since handler has specified no preference for wrapper type the most generic of all will do and any wrapped event containing a matching event should be dispatched to this handler.
            static readonly Type WrapperType = typeof(MessageTypes.IWrapperEvent<>).MakeGenericType(typeof(THandledEvent));
            readonly Action<THandledEvent> _handler;
            public RegisteredHandler(Action<THandledEvent> handler) => _handler = handler;
            internal override Action<object>? TryCreateHandlerFor(Type eventType)
            {
                if(typeof(THandledEvent).IsAssignableFrom(eventType))
                {
                    return @event => _handler((THandledEvent)@event);
                } else if(WrapperType.IsAssignableFrom(eventType))
                {
                    return @event => _handler((THandledEvent)((MessageTypes.IWrapperEvent<MessageTypes.IEvent>)@event).Event);
                    ;
                } else
                {
                    return null;
                }
            }
        }

        class RegisteredWrappedHandler<THandledWrapperEvent, TWrappedEvent> : RegisteredHandler
            where TWrappedEvent : TEvent
            where THandledWrapperEvent : MessageTypes.IWrapperEvent<TWrappedEvent>
        {
            readonly Action<THandledWrapperEvent> _handler;
            static readonly Func<object, THandledWrapperEvent> WrapperConstructor;

            static RegisteredWrappedHandler()
            {
                var closedWrapperEventType = typeof(THandledWrapperEvent);
                var openWrapperEventType = closedWrapperEventType.GetGenericTypeDefinition();

                var openWrapperImplementationType = CreateGenericWrapperEventType(openWrapperEventType);
                var wrappedEventType = closedWrapperEventType.GenericTypeArguments[0];
                var closedWrapperImplementationType = openWrapperImplementationType.MakeGenericType(wrappedEventType);

                var constructor = Constructor.Compile.ForReturnType(closedWrapperImplementationType).WithArgumentTypes(wrappedEventType);

                //Urgent: Performance: DynamicInvoke here is not going to be terribly fast.
                WrapperConstructor = @event => (THandledWrapperEvent)constructor.DynamicInvoke(@event).NotNull();
            }

            public RegisteredWrappedHandler(Action<THandledWrapperEvent> handler) => _handler = handler;
            internal override Action<object>? TryCreateHandlerFor(Type eventType)
            {
                if(typeof(THandledWrapperEvent).IsAssignableFrom(eventType))
                {
                    return @event => _handler((THandledWrapperEvent)@event);
                } else if(typeof(TWrappedEvent).IsAssignableFrom(eventType))
                {
                    return @event => _handler(WrapperConstructor((TWrappedEvent)@event));
                } else
                {
                    return null;
                }
            }

            static IReadOnlyDictionary<Type, Type> _createdWrapperTypes = new Dictionary<Type, Type>();
            static readonly MonitorCE Monitor = MonitorCE.WithDefaultTimeout();
            static Type CreateGenericWrapperEventType(Type wrapperEventType)
            {
                if(_createdWrapperTypes.TryGetValue(wrapperEventType, out var cachedWrapperImplementation))
                {
                    return cachedWrapperImplementation;
                }

                return Monitor.Update(() =>
                {
                    if(!wrapperEventType.IsInterface) throw new ArgumentException("Must be an interface", $"{nameof(wrapperEventType)}");

                    if(wrapperEventType != typeof(MessageTypes.IWrapperEvent<>)
                    && wrapperEventType.GetInterfaces().All(iface => iface != typeof(MessageTypes.IWrapperEvent<>).MakeGenericType(wrapperEventType.GetGenericArguments()[0])))
                        throw new ArgumentException($"Must implement {typeof(MessageTypes.IWrapperEvent<>).FullName}", $"{nameof(wrapperEventType)}");

                    var wrappedEventType = wrapperEventType.GetGenericArguments()[0];

                    var requiredEventInterface = wrappedEventType.GetGenericParameterConstraints().Single(constraint => constraint.IsInterface && typeof(MessageTypes.IEvent).IsAssignableFrom(constraint));

                    var genericWrapperEventType = AssemblyBuilderCE.Module.Update(module =>
                    {
                        TypeBuilder wrapperEventBuilder = module.DefineType(
                            name: $"{wrapperEventType}_ilgen_impl",
                            attr: TypeAttributes.Public,
                            parent: null,
                            interfaces: new[] {wrapperEventType});

                        GenericTypeParameterBuilder wrappedEventTypeParameter = wrapperEventBuilder.DefineGenericParameters("TWrappedEvent")[0];

                        wrappedEventTypeParameter.SetInterfaceConstraints(requiredEventInterface);

                        var (wrappedEventField, _) = wrapperEventBuilder.ImplementProperty(nameof(MessageTypes.IWrapperEvent<IAggregateEvent>.Event), wrappedEventTypeParameter);

                        wrapperEventBuilder.ImplementConstructor(wrappedEventField);

                        return wrapperEventBuilder.CreateType().NotNull();
                    });

                    ThreadSafe.AddToCopyAndReplace(ref _createdWrapperTypes, wrapperEventType, genericWrapperEventType);

                    return genericWrapperEventType;
                });
            }
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

            RegistrationBuilder ForWrapped<TWrapperEvent, TWrappedEvent>(Action<TWrapperEvent> handler)
                where TWrappedEvent : TEvent, MessageTypes.IEvent
                where TWrapperEvent : MessageTypes.IWrapperEvent<TWrappedEvent>
            {
                _owner._handlers.Add(new RegisteredWrappedHandler<TWrapperEvent, TWrappedEvent>(handler));
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

            IEventHandlerRegistrar<TEvent> IEventHandlerRegistrar<TEvent>.BeforeHandlers<THandledEvent>(Action<THandledEvent> runBeforeHandlers) { return BeforeHandlers(e => runBeforeHandlers((THandledEvent)e)); }

            IEventHandlerRegistrar<TEvent> IEventHandlerRegistrar<TEvent>.AfterHandlers<THandledEvent>(Action<THandledEvent> runAfterHandlers) { return AfterHandlers(e => runAfterHandlers((THandledEvent)e)); }

            IEventHandlerRegistrar<TEvent> IEventHandlerRegistrar<TEvent>.IgnoreUnhandled<THandledEvent>() => IgnoreUnhandled<THandledEvent>();

            IEventHandlerRegistrar<TEvent> IEventHandlerRegistrar<TEvent>.For<THandledEvent>(Action<THandledEvent> handler) => For(handler);

            IEventHandlerRegistrar<TEvent> IEventHandlerRegistrar<TEvent>.ForWrapped<TWrapperEvent, TWrappedEvent>(Action<TWrapperEvent> handler) => ForWrapped<TWrapperEvent, TWrappedEvent>(handler);

            #endregion
        }

        Dictionary<Type, Action<object>[]> _typeToHandlerCache = new Dictionary<Type, Action<object>[]>();
        int _cachedTotalHandlers;
        // ReSharper disable once StaticMemberInGenericType
        static readonly Action<object>[] NullHandlerList = Array.Empty<Action<object>>();

        Action<object>[] GetHandlers(Type type, bool validateHandlerExists = true)
        {
            if(_cachedTotalHandlers != _totalHandlers)
            {
                _cachedTotalHandlers = _totalHandlers;
                _typeToHandlerCache = new Dictionary<Type, Action<object>[]>();
            }

            if(_typeToHandlerCache.TryGetValue(type, out var arrayResult))
            {
                return arrayResult;
            }

            var result = new List<Action<object>>();
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

        public bool HandlesEvent<THandled>() => GetHandlers(typeof(THandled), validateHandlerExists: false).Any();
        public bool Handles(IAggregateEvent @event) => GetHandlers(@event.GetType(), validateHandlerExists: false).Any();
    }

    public class EventUnhandledException : Exception
    {
        public EventUnhandledException(Type handlerType, Type eventType)
            : base($@"{handlerType} does not handle nor ignore incoming event {eventType}") {}
    }
}
