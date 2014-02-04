using Composable.CQRS.EventSourcing;
using Composable.System;
using Composable.System.Linq;
using NServiceBus;

namespace Composable.CQRS.EventHandling
{
    /// <summary>
    /// Calls all matching handlers in the order they were registered when an event is received.
    /// Handlers should be registered using the RegisterHandlers method in the constructor of the inheritor.
    /// </summary>
    public abstract class CallsMatchingHandlersInRegistrationOrderEventHandler<TEvent> : IHandleMessages<TEvent>
        where TEvent : IAggregateRootEvent
    {
        private readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TEvent> _eventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TEvent>(); 

        ///<summary>Registers handlers for the incoming events. All matching handlers will be called in the order they were registered.</summary>
        protected CallMatchingHandlersInRegistrationOrderEventDispatcher<TEvent>.RegistrationBuilder RegisterHandlers()
        {
            return _eventDispatcher.RegisterHandlers();
        }

        public virtual void Handle(TEvent evt)
        {
           _eventDispatcher.Dispatch(evt);
        }
    }
}
