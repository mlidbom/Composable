using System;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Events;
using JetBrains.Annotations;

namespace Composable.Persistence.EventStore.Query.Models.AggregateRoots
{
    public abstract partial class SelfGeneratingQueryModel<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>
        where TAggregateRoot : SelfGeneratingQueryModel<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>
        where TAggregateRootBaseEventInterface : class, IAggregateRootEvent
        where TAggregateRootBaseEventClass : AggregateRootEvent, TAggregateRootBaseEventInterface
    {
        public abstract partial class Component<TComponent, TComponentBaseEventClass, TComponentBaseEventInterface>
            where TComponentBaseEventInterface : class, TAggregateRootBaseEventInterface
            where TComponentBaseEventClass : TAggregateRootBaseEventClass, TComponentBaseEventInterface
            where TComponent : Component<TComponent, TComponentBaseEventClass, TComponentBaseEventInterface>
        {
            [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
            public abstract class NestedComponent<TNestedComponent, TNestedComponentBaseEventClass, TNestedComponentBaseEventInterface> :
                SelfGeneratingQueryModel<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>.
                    Component<TNestedComponent, TNestedComponentBaseEventClass, TNestedComponentBaseEventInterface>
                where TNestedComponentBaseEventInterface : class, TComponentBaseEventInterface
                where TNestedComponentBaseEventClass : TComponentBaseEventClass, TNestedComponentBaseEventInterface
                where TNestedComponent : NestedComponent<TNestedComponent, TNestedComponentBaseEventClass, TNestedComponentBaseEventInterface>
            {
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
