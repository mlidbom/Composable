using System;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Events;

namespace Composable.Persistence.EventStore.Aggregates
{
    public abstract partial class Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent>
        where TAggregate : Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent>
        where TAggregateEvent : class, IAggregateEvent
        where TAggregateEventImplementation : AggregateEvent, TAggregateEvent
    {
        public abstract partial class Component<TComponent, TComponentEventImplementation, TComponentEvent>
            where TComponentEvent : class, TAggregateEvent
            where TComponentEventImplementation : TAggregateEventImplementation, TComponentEvent
            where TComponent : Component<TComponent, TComponentEventImplementation, TComponentEvent>
        {
            static Component() => AggregateTypeValidator<TComponent, TComponentEventImplementation, TComponentEvent>.AssertStaticStructureIsValid();

            readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentEvent> _eventAppliersEventDispatcher =
                new CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentEvent>();
            readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentEvent> _eventHandlersEventDispatcher =
                new CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentEvent>();
            readonly Action<TComponentEventImplementation> _raiseEventThroughParent;

            IUtcTimeTimeSource TimeSource { get; set; }

            void ApplyEvent(TComponentEvent @event)
            {
                _eventAppliersEventDispatcher.Dispatch(@event);
            }

            protected Component(TAggregate aggregateRoot)
                : this(
                    timeSource: aggregateRoot.TimeSource,
                    raiseEventThroughParent: aggregateRoot.Publish,
                    appliersRegistrar: aggregateRoot.RegisterEventAppliers(),
                    registerEventAppliers: true)
            {}

            internal Component(IUtcTimeTimeSource timeSource, Action<TComponentEventImplementation> raiseEventThroughParent, IEventHandlerRegistrar<TComponentEvent> appliersRegistrar, bool registerEventAppliers)
            {
                TimeSource = timeSource;
                _raiseEventThroughParent = raiseEventThroughParent;
                _eventHandlersEventDispatcher.Register()
                                            .IgnoreUnhandled<TComponentEvent>();

                if(registerEventAppliers)
                {
                    appliersRegistrar
                                 .For<TComponentEvent>(ApplyEvent);
                }
            }

            protected virtual void Publish(TComponentEventImplementation @event)
            {
                _raiseEventThroughParent(@event);
                _eventHandlersEventDispatcher.Dispatch(@event);
            }

            protected IEventHandlerRegistrar<TComponentEvent> RegisterEventAppliers() => _eventAppliersEventDispatcher.Register();

            // ReSharper disable once UnusedMember.Global todo: tests
            protected IEventHandlerRegistrar<TComponentEvent> RegisterEventHandlers() => _eventHandlersEventDispatcher.Register();
        }
    }
}
