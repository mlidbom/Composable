// ReSharper disable UnusedMember.Global todo: refactor usages of concrete classes to use these interfaces. Then remove unused.

using System;

namespace Composable.Messaging.Events
{
    ///<summary>
    /// <para>An unrestricted and therefore unsafe version of a <see cref="IEventHandlerRegistrar{TEvent}"/></para>
    /// <para>Gives you the ability to register any type, but therefor does not in any way help you avoid mistakes.</para>
    /// <para>Whenever possible use <see cref="IEventHandlerRegistrar{TEvent}"/> instead.</para>
    /// <para>To get an instance of this interface, Call the extension on <see cref="IEventHandlerRegistrar{TEvent}"/> <see cref="EventHandlerRegistrar.MakeGeneric{TEvent}"/></para>
    /// 
    /// </summary>
    interface IGenericEventHandlerRegistrar
    {
        IGenericEventHandlerRegistrar ForGenericEvent<THandledEvent>(Action<THandledEvent> handler); // todo: Write tests
    }

    ///<summary>This registrar was created by upcasting an existing registrar. how the implementation of this is hidden gives some help ensuring that it is safe to use this.</summary>
    public interface IUpCastEventHandlerRegistrar<in TEvent>
        where TEvent : class
    {
        ///<summary>Registers a handler for any event that implements THandledEvent. All matching handlers will be called in the order they were registered.</summary>
        IUpCastEventHandlerRegistrar<TEvent> For<THandledEvent>(Action<THandledEvent> handler) where THandledEvent : TEvent;

        ///<summary>Lets you register handlers for event interfaces that may be defined outside of the event hierarchy you specify with TEvent.
        /// Useful for listening to generic events such as IAggregateCreatedEvent or IAggregateDeletedEvent
        /// Be aware that the concrete event received MUST still actually inherit TEvent or there will be an InvalidCastException
        /// </summary>
        IUpCastEventHandlerRegistrar<TEvent> ForGenericEvent<THandledEvent>(Action<THandledEvent> handler);
    }

    public interface IEventHandlerRegistrar<in TEvent>
        where TEvent : class
    {
        ///<summary>Registers a handler for any event that implements THandledEvent. All matching handlers will be called in the order they were registered.</summary>
        IEventHandlerRegistrar<TEvent> For<THandledEvent>(Action<THandledEvent> handler) where THandledEvent : TEvent;

        ///<summary>Lets you register handlers for event interfaces that may be defined outside of the event hierarchy you specify with TEvent.
        /// Useful for listening to generic events such as IAggregateCreatedEvent or IAggregateDeletedEvent
        /// Be aware that the concrete event received MUST still actually inherit TEvent or there will be an InvalidCastException
        /// </summary>
        IEventHandlerRegistrar<TEvent> ForGenericEvent<THandledEvent>(Action<THandledEvent> handler);

        IEventHandlerRegistrar<TEvent> BeforeHandlers<THandledEvent>(Action<THandledEvent> runBeforeHandlers) where THandledEvent : TEvent;
        IEventHandlerRegistrar<TEvent> AfterHandlers<THandledEvent>(Action<THandledEvent> runAfterHandlers) where THandledEvent : TEvent;
        IEventHandlerRegistrar<TEvent> IgnoreUnhandled<TIgnored>() where TIgnored : TEvent;
    }

    public static class EventHandlerRegistrar
    {
        internal static IEventHandlerRegistrar<TEvent> BeforeHandlers<TEvent>
            (this IEventHandlerRegistrar<TEvent> @this, Action<TEvent> handler) where TEvent : class => @this.BeforeHandlers(handler);

        internal static IEventHandlerRegistrar<TEvent> AfterHandlers<TEvent>
            (this IEventHandlerRegistrar<TEvent> @this, Action<TEvent> handler) where TEvent : class => @this.AfterHandlers(handler);

        public static IEventHandlerRegistrar<TDownCastEvent> DownCast<TEvent, TDownCastEvent>(this IEventHandlerRegistrar<TEvent> @this)
            where TEvent : class
            where TDownCastEvent : class, TEvent => @this;

        internal static IGenericEventHandlerRegistrar MakeGeneric<TEvent>(this IEventHandlerRegistrar<TEvent> @this)
            where TEvent : class => new GenericEventHandlerRegistrar<TEvent>(@this);

        class GenericEventHandlerRegistrar<TEvent> : IGenericEventHandlerRegistrar where TEvent : class
        {
            readonly IEventHandlerRegistrar<TEvent> _innerRegistrar;
            internal GenericEventHandlerRegistrar(IEventHandlerRegistrar<TEvent> innerRegistrar) => _innerRegistrar = innerRegistrar;

            public IGenericEventHandlerRegistrar ForGenericEvent<THandledEvent>(Action<THandledEvent> handler)
            {
                _innerRegistrar.ForGenericEvent(handler);
                return this;
            }
        }

        public static IUpCastEventHandlerRegistrar<TUpCastEvent> UpCast<TEvent, TUpCastEvent>(this IEventHandlerRegistrar<TEvent> @this)
            where TUpCastEvent : class
            where TEvent : class, TUpCastEvent => new UpCastEventHandlerRegistrar<TEvent, TUpCastEvent>(@this);

        class UpCastEventHandlerRegistrar<TEvent, TUpCastEvent> : IUpCastEventHandlerRegistrar<TUpCastEvent>
            where TUpCastEvent : class
            where TEvent : class, TUpCastEvent
        {
            readonly IEventHandlerRegistrar<TEvent> _innerRegistrar;
            internal UpCastEventHandlerRegistrar(IEventHandlerRegistrar<TEvent> innerRegistrar) => _innerRegistrar = innerRegistrar;

            public IUpCastEventHandlerRegistrar<TUpCastEvent> For<THandledEvent>(Action<THandledEvent> handler) where THandledEvent : TUpCastEvent => ForGenericEvent(handler);

            public IUpCastEventHandlerRegistrar<TUpCastEvent> ForGenericEvent<THandledEvent>(Action<THandledEvent> handler)
            {
                _innerRegistrar.ForGenericEvent(handler);
                return this;
            }
        }
    }
}
