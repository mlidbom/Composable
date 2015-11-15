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
        protected readonly Func<ITimeSource> TimeSourceFetcher;
        private readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventClass> _eventAppliersEventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventClass>();
        private readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventClass> _eventHandlersEventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventClass>();

        protected AggregateRootComponent(
            TAggregateRoot aggregateRoot,
            Action<TComponentBaseEventClass> raiseEvent,
            Func<ITimeSource> timeSourceFetcher)
        {
            Contract.Requires(aggregateRoot != null);
            Contract.Requires(raiseEvent != null);
            Contract.Requires(timeSourceFetcher != null);

            _eventHandlersEventDispatcher.RegisterHandlers()
                .IgnoreUnhandled<TComponentBaseEventClass>();

            AggregateRoot = aggregateRoot;
            _raiseEvent = raiseEvent;
            TimeSourceFetcher = timeSourceFetcher;
        }

        protected TAggregateRoot AggregateRoot { get; private set; }
        protected ITimeSource TimeSource => TimeSourceFetcher();

        protected void ApplyEvent(TComponentBaseEventClass @event)
        {
            _eventAppliersEventDispatcher.Dispatch(@event);
        }

        protected void RaiseEvent(TComponentBaseEventClass @event)
        {
            _raiseEvent(@event);
            _eventHandlersEventDispatcher.Dispatch(@event);
        }

        protected CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventClass>.RegistrationBuilder RegisterEventAppliers()
        {
            return _eventAppliersEventDispatcher.RegisterHandlers();
        }

        protected CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventClass>.RegistrationBuilder RegisterEventHandlers()
        {
            return _eventHandlersEventDispatcher.RegisterHandlers();
        }
    }
}