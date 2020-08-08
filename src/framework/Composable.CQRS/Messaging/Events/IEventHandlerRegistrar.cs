using System;

namespace Composable.Messaging.Events
{

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

    static class EventHandlerRegistrar
    {
        public static IEventHandlerRegistrar<TEvent> BeforeHandlers<TEvent>
            (this IEventHandlerRegistrar<TEvent> @this, Action<TEvent> handler) where TEvent : class => @this.BeforeHandlers(handler);

        public static IEventHandlerRegistrar<TEvent> AfterHandlers<TEvent>
            (this IEventHandlerRegistrar<TEvent> @this, Action<TEvent> handler) where TEvent : class => @this.AfterHandlers(handler);

    }
}
