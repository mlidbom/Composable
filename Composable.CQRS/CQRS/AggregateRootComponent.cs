using System;
using System.Diagnostics.Contracts;
using Composable.CQRS.EventHandling;
using Composable.CQRS.EventSourcing;
using Composable.GenericAbstractions.Time;

namespace Composable.CQRS
{
    public abstract class AggregateRootComponent<TAggregateRoot, TBaseEvent, TAggregateRootBaseEvent>
        where TBaseEvent : TAggregateRootBaseEvent
        where TAggregateRoot : AggregateRoot<TAggregateRoot, TAggregateRootBaseEvent>
        where TAggregateRootBaseEvent : IAggregateRootEvent
    {
        private readonly Action<TBaseEvent> _raiseEvent;
        protected readonly Func<ITimeSource> TimeSourceFetcher;
        private readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TBaseEvent> _eventAppliersEventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TBaseEvent>();
        private readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TBaseEvent> _eventHandlersEventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TBaseEvent>();

        protected AggregateRootComponent(
            TAggregateRoot aggregateRoot,
            Action<TBaseEvent> raiseEvent,
            Func<ITimeSource> timeSourceFetcher)
        {
            Contract.Requires(aggregateRoot != null);
            Contract.Requires(raiseEvent != null);
            Contract.Requires(timeSourceFetcher != null);

            _eventHandlersEventDispatcher.RegisterHandlers()
                .IgnoreUnhandled<TBaseEvent>();

            AggregateRoot = aggregateRoot;
            _raiseEvent = raiseEvent;
            TimeSourceFetcher = timeSourceFetcher;
        }

        protected TAggregateRoot AggregateRoot { get; private set; }
        protected ITimeSource TimeSource => TimeSourceFetcher();

        protected void ApplyEvent(TBaseEvent @event)
        {
            _eventAppliersEventDispatcher.Dispatch(@event);
        }

        protected void RaiseEvent(TBaseEvent @event)
        {
            _raiseEvent(@event);
            _eventHandlersEventDispatcher.Dispatch(@event);
        }

        protected CallMatchingHandlersInRegistrationOrderEventDispatcher<TBaseEvent>.RegistrationBuilder RegisterEventAppliers()
        {
            return _eventAppliersEventDispatcher.RegisterHandlers();
        }

        protected CallMatchingHandlersInRegistrationOrderEventDispatcher<TBaseEvent>.RegistrationBuilder RegisterEventHandlers()
        {
            return _eventHandlersEventDispatcher.RegisterHandlers();
        }
    }
}