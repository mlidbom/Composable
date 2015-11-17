using System;
using System.Diagnostics.Contracts;
using Composable.CQRS.EventHandling;
using Composable.CQRS.EventSourcing;
using Composable.GenericAbstractions.Time;

namespace Composable.CQRS
{
    public abstract class AggregateRootComponent<TAggregateRoot, TComponentBaseEventClass, TComponentBaseEventInterface, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>        
        where TAggregateRoot : AggregateRoot<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>
        where TAggregateRootBaseEventInterface : IAggregateRootEvent
        where TAggregateRootBaseEventClass : AggregateRootEvent, TAggregateRootBaseEventInterface
        where TComponentBaseEventInterface : TAggregateRootBaseEventInterface
        where TComponentBaseEventClass : TAggregateRootBaseEventClass, TComponentBaseEventInterface
    {
        private readonly Action<TComponentBaseEventClass> _raiseEvent;
        private readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface> _eventAppliersEventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface>();
        private readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface> _eventHandlersEventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface>();

        protected AggregateRootComponent(
            TAggregateRoot aggregateRoot,
            Action<TComponentBaseEventClass> raiseEvent)
        {
            Contract.Requires(aggregateRoot != null);
            Contract.Requires(raiseEvent != null);

            _eventHandlersEventDispatcher.RegisterHandlers()
                .IgnoreUnhandled<TComponentBaseEventClass>();

            AggregateRoot = aggregateRoot;
            _raiseEvent = raiseEvent;
        }

        protected ITimeSource TimeSource => AggregateRoot.TimeSource;
        protected TAggregateRoot AggregateRoot { get; private set; }

        protected void ApplyEvent(TComponentBaseEventInterface @event)
        {
            _eventAppliersEventDispatcher.Dispatch(@event);
        }

        protected void RaiseEvent(TComponentBaseEventClass @event)
        {
            _raiseEvent(@event);
            _eventHandlersEventDispatcher.Dispatch(@event);
        }

        protected CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface>.RegistrationBuilder RegisterEventAppliers()
        {
            return _eventAppliersEventDispatcher.RegisterHandlers();
        }

        protected CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface>.RegistrationBuilder RegisterEventHandlers()
        {
            return _eventHandlersEventDispatcher.RegisterHandlers();
        }
    }
}