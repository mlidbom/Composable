using System;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Events;
using JetBrains.Annotations;

namespace Composable.Persistence.EventStore.Aggregates
{
    public abstract partial class Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent>
        where TAggregate : Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent>
        where TAggregateEvent : class, IAggregateRootEvent
        where TAggregateEventImplementation : AggregateRootEvent, TAggregateEvent
    {
        public abstract partial class Component<TComponent, TComponentEventImplementation, TComponentEvent>
            where TComponentEvent : class, TAggregateEvent
            where TComponentEventImplementation : TAggregateEventImplementation, TComponentEvent
            where TComponent : Component<TComponent, TComponentEventImplementation, TComponentEvent>
        {
            [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
            public abstract class NestedComponent<TNestedComponent, TNestedComponentBaseEventClass, TNestedComponentBaseEventInterface> :
                Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent>.
                    Component<TNestedComponent, TNestedComponentBaseEventClass, TNestedComponentBaseEventInterface>
                where TNestedComponentBaseEventInterface : class, TComponentEvent
                where TNestedComponentBaseEventClass : TComponentEventImplementation, TNestedComponentBaseEventInterface
                where TNestedComponent : NestedComponent<TNestedComponent, TNestedComponentBaseEventClass, TNestedComponentBaseEventInterface>
            {
                static NestedComponent() => AggregateTypeValidator<TNestedComponent, TNestedComponentBaseEventClass, TNestedComponentBaseEventInterface>.AssertStaticStructureIsValid();

                protected NestedComponent(TComponent parent)
                    : base(parent.TimeSource, parent.Publish, parent.RegisterEventAppliers(), registerEventAppliers: true) {}

                protected NestedComponent
                    (IUtcTimeTimeSource timeSource,
                     Action<TNestedComponentBaseEventClass> raiseEventThroughParent,
                     IEventHandlerRegistrar<TNestedComponentBaseEventInterface> appliersRegistrar,
                     bool registerEventAppliers) : base(timeSource, raiseEventThroughParent, appliersRegistrar, registerEventAppliers) {}
            }
        }
    }
}
