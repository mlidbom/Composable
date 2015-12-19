using Composable.CQRS.EventHandling;
using Composable.GenericAbstractions.Time;

namespace Composable.CQRS.EventSourcing
{
    public abstract partial class AggregateRoot<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>
    {
        public abstract partial class Component<TComponent, TComponentBaseEventClass, TComponentBaseEventInterface>
            where TComponentBaseEventInterface : TAggregateRootBaseEventInterface
            where TComponentBaseEventClass : TAggregateRootBaseEventClass, TComponentBaseEventInterface
            where TComponent : Component<TComponent, TComponentBaseEventClass, TComponentBaseEventInterface>
        {
            private readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface> _eventAppliersEventDispatcher =
                new CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface>();
            internal readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface> EventHandlersEventDispatcher =
                new CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface>();

            protected IUtcTimeTimeSource TimeSource => AggregateRoot.TimeSource;
            private TAggregateRoot AggregateRoot { get; set; }

            internal void ApplyEvent(TComponentBaseEventInterface @event) { _eventAppliersEventDispatcher.Dispatch(@event); }

            protected Component(TAggregateRoot aggregateRoot) : this(aggregateRoot: aggregateRoot, registerEventAppliers: true) { }

            internal Component(TAggregateRoot aggregateRoot, bool registerEventAppliers)
            {
                AggregateRoot = aggregateRoot;
                EventHandlersEventDispatcher.Register()
                                            .IgnoreUnhandled<TComponentBaseEventInterface>();

                if(registerEventAppliers)
                {
                    AggregateRoot.RegisterEventAppliers()
                                 .For<TComponentBaseEventInterface>(ApplyEvent);
                }
            }

            protected virtual void RaiseEvent(TComponentBaseEventClass @event)
            {
                AggregateRoot.RaiseEvent(@event);
                EventHandlersEventDispatcher.Dispatch(@event);
            }

            protected CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface>.RegistrationBuilder RegisterEventAppliers()
            {
                return _eventAppliersEventDispatcher.RegisterHandlers();
            }

            protected CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface>.RegistrationBuilder RegisterEventHandlers()
            {
                return EventHandlersEventDispatcher.RegisterHandlers();
            }
        }
    }
}
