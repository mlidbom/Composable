using Composable.Messaging.Events;
using JetBrains.Annotations;

namespace Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels
{
    public abstract partial class SelfGeneratingQueryModel<TAggregate, TAggregateEvent>
        where TAggregate : SelfGeneratingQueryModel<TAggregate, TAggregateEvent>
        where TAggregateEvent : class, IAggregateRootEvent
    {
        public abstract partial class Component<TComponent, TComponentEvent>
            where TComponentEvent : class, TAggregateEvent
            where TComponent : Component<TComponent, TComponentEvent>
        {
            [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
            public abstract class NestedComponent<TNestedComponent, TNestedComponentBaseEventInterface> : Component<TNestedComponent, TNestedComponentBaseEventInterface>
                where TNestedComponentBaseEventInterface : class, TComponentEvent
                where TNestedComponent : NestedComponent<TNestedComponent, TNestedComponentBaseEventInterface>
            {
                protected NestedComponent(TComponent parent) : base(parent.RegisterEventAppliers(), registerEventAppliers: true) {}

                protected NestedComponent(IEventHandlerRegistrar<TNestedComponentBaseEventInterface> appliersRegistrar,
                                          bool registerEventAppliers) : base(appliersRegistrar, registerEventAppliers) {}
            }
        }
    }
}
