using System;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Events;
using JetBrains.Annotations;

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
            [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
            public abstract class NestedComponent<TNestedComponent, TNestedComponentEventImplementation, TNestedComponentEvent> :
                    Component<TNestedComponent, TNestedComponentEventImplementation, TNestedComponentEvent>
                where TNestedComponentEvent : class, TComponentEvent
                where TNestedComponentEventImplementation : TComponentEventImplementation, TNestedComponentEvent
                where TNestedComponent : NestedComponent<TNestedComponent, TNestedComponentEventImplementation, TNestedComponentEvent>
            {
                static NestedComponent() => AggregateTypeValidator<TNestedComponent, TNestedComponentEventImplementation, TNestedComponentEvent>.AssertStaticStructureIsValid();

                protected NestedComponent(TComponent parent)
                    : base(parent.TimeSource, parent.Publish, parent.RegisterEventAppliers(), registerEventAppliers: true) {}

                protected NestedComponent
                    (IUtcTimeTimeSource timeSource,
                     Action<TNestedComponentEventImplementation> raiseEventThroughParent,
                     IEventHandlerRegistrar<TNestedComponentEvent> appliersRegistrar,
                     bool registerEventAppliers) : base(timeSource, raiseEventThroughParent, appliersRegistrar, registerEventAppliers) {}
            }
        }
    }
}
