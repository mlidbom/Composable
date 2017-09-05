using Composable.Persistence.EventStore;

namespace Composable.Messaging.Events
{
    /// <summary>
    /// Calls all matching handlers in the order they were registered when an event is received.
    /// Handlers should be registered using the RegisterHandlers method in the constructor of the inheritor.
    /// </summary>
    public abstract class CallsMatchingHandlersInRegistrationOrderEventDispatcherSpecification<TEvent> : IEventSubscriber<TEvent>
        where TEvent : class, IAggregateRootEvent
    {
        readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TEvent> _eventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TEvent>();

        ///<summary>Registers handlers for the incoming events. All matching handlers will be called in the order they were registered.</summary>
        protected IEventHandlerRegistrar<TEvent> RegisterHandlers() => _eventDispatcher.RegisterHandlers();

        public virtual void Handle(TEvent evt) { _eventDispatcher.Dispatch(evt); }
    }
}
