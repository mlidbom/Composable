// ReSharper disable UnusedMember.Global todo: refactor usages of concrete classes to use these interfaces. Then remove unused.

using System;

namespace Composable.Messaging.Events
{
    ///<summary>
    /// <para>An unrestricted and therefore unsafe version of a <see cref="IEventHandlerRegistrar{TBaseEvent}"/></para>
    /// <para>Gives you the ability to register any type, but therefor does not in any way help you avoid mistakes.</para>
    /// <para>Whenever possible use <see cref="IEventHandlerRegistrar{TBaseEvent}"/> instead.</para>
    /// <para>To get an instance of this interface, Call the extension on <see cref="IEventHandlerRegistrar{TBaseEvent}"/> <see cref="EventHandlerRegistrar.MakeGeneric{TBaseEvent}"/></para>
    /// 
    /// </summary>
    interface IGenericEventHandlerRegistrar
    {
        IGenericEventHandlerRegistrar ForGenericEvent<THandledEvent>(Action<THandledEvent> handler); // todo: Write tests
    }

    ///<summary>This registrar was created by upcasting an existing registrar. how the implementation of this is hidden gives some help ensuring that it is safe to use this.</summary>
    interface IUpCastEventHandlerRegistrar<in TBaseEvent>
        where TBaseEvent : class
    {
        ///<summary>Registers a handler for any event that implements THandledEvent. All matching handlers will be called in the order they were registered.</summary>
        IUpCastEventHandlerRegistrar<TBaseEvent> For<THandledEvent>(Action<THandledEvent> handler) where THandledEvent : TBaseEvent;

        ///<summary>Lets you register handlers for event interfaces that may be defined outside of the event hierarchy you specify with TEvent.
        /// Useful for listening to generic events such as IAggregateRootCreatedEvent or IAggregateRootDeletedEvent
        /// Be aware that the concrete event received MUST still actually inherit TEvent or there will be an InvalidCastException
        /// </summary>
        IUpCastEventHandlerRegistrar<TBaseEvent> ForGenericEvent<THandledEvent>(Action<THandledEvent> handler);
    }

    public interface IEventHandlerRegistrar<in TBaseEvent>
        where TBaseEvent : class
    {
        ///<summary>Registers a handler for any event that implements THandledEvent. All matching handlers will be called in the order they were registered.</summary>
        IEventHandlerRegistrar<TBaseEvent> For<THandledEvent>(Action<THandledEvent> handler) where THandledEvent : TBaseEvent;

        ///<summary>Lets you register handlers for event interfaces that may be defined outside of the event hierarchy you specify with TEvent.
        /// Useful for listening to generic events such as IAggregateRootCreatedEvent or IAggregateRootDeletedEvent
        /// Be aware that the concrete event received MUST still actually inherit TEvent or there will be an InvalidCastException
        /// </summary>
        IEventHandlerRegistrar<TBaseEvent> ForGenericEvent<THandledEvent>(Action<THandledEvent> handler);

        IEventHandlerRegistrar<TBaseEvent> BeforeHandlers<THandledEvent>(Action<THandledEvent> runBeforeHandlers) where THandledEvent : TBaseEvent;
        IEventHandlerRegistrar<TBaseEvent> AfterHandlers<THandledEvent>(Action<THandledEvent> runAfterHandlers) where THandledEvent : TBaseEvent;
        IEventHandlerRegistrar<TBaseEvent> IgnoreUnhandled<T>();
    }

    static class EventHandlerRegistrar
    {
        public static IEventHandlerRegistrar<TBaseEvent> BeforeHandlers<TBaseEvent>
            (this IEventHandlerRegistrar<TBaseEvent> @this, Action<TBaseEvent> handler) where TBaseEvent : class => @this.BeforeHandlers(handler);

        public static IEventHandlerRegistrar<TBaseEvent> AfterHandlers<TBaseEvent>
            (this IEventHandlerRegistrar<TBaseEvent> @this, Action<TBaseEvent> handler) where TBaseEvent : class => @this.AfterHandlers(handler);

        public static IEventHandlerRegistrar<TNewBaseEvent> DownCast<TBaseEvent, TNewBaseEvent>(this IEventHandlerRegistrar<TBaseEvent> @this)
            where TBaseEvent : class
            where TNewBaseEvent : class, TBaseEvent => @this;

        public static IGenericEventHandlerRegistrar MakeGeneric<TBaseEvent>(this IEventHandlerRegistrar<TBaseEvent> @this)
            where TBaseEvent : class => new GenericEventHandlerRegistrar<TBaseEvent>(@this);

        class GenericEventHandlerRegistrar<TBaseEventInterface> : IGenericEventHandlerRegistrar
            where TBaseEventInterface : class
        {
            readonly IEventHandlerRegistrar<TBaseEventInterface> _innerRegistrar;
            public GenericEventHandlerRegistrar(IEventHandlerRegistrar<TBaseEventInterface> innerRegistrar) => _innerRegistrar = innerRegistrar;

            public IGenericEventHandlerRegistrar ForGenericEvent<THandledEvent>(Action<THandledEvent> handler)
            {
                _innerRegistrar.ForGenericEvent(handler);
                return this;
            }
        }

        public static IUpCastEventHandlerRegistrar<TNewBaseEvent> UpCast<TBaseEvent, TNewBaseEvent>(this IEventHandlerRegistrar<TBaseEvent> @this)
            where TBaseEvent : class
            where TNewBaseEvent : class, TBaseEvent => new UpCastEventHandlerRegistrar<TBaseEvent, TNewBaseEvent>(@this);

        class UpCastEventHandlerRegistrar<TBaseEvent, TNewBaseEvent> : IUpCastEventHandlerRegistrar<TNewBaseEvent>
            where TBaseEvent : class
            where TNewBaseEvent : class, TBaseEvent
        {
            readonly IEventHandlerRegistrar<TBaseEvent> _innerRegistrar;
            public UpCastEventHandlerRegistrar(IEventHandlerRegistrar<TBaseEvent> innerRegistrar) => _innerRegistrar = innerRegistrar;

            public IUpCastEventHandlerRegistrar<TNewBaseEvent> For<THandledEvent>(Action<THandledEvent> handler) where THandledEvent : TNewBaseEvent => ForGenericEvent(handler);

            public IUpCastEventHandlerRegistrar<TNewBaseEvent> ForGenericEvent<THandledEvent>(Action<THandledEvent> handler)
            {
                _innerRegistrar.ForGenericEvent(handler);
                return this;
            }
        }
    }
}
