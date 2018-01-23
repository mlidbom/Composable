using Composable.Messaging.Events;
using JetBrains.Annotations;

namespace Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels
{
    public abstract partial class SelfGeneratingQueryModel<TAggregateRoot, TAggregateRootBaseEventInterface>
        where TAggregateRoot : SelfGeneratingQueryModel<TAggregateRoot, TAggregateRootBaseEventInterface>
        where TAggregateRootBaseEventInterface : class, IAggregateRootEvent
    {
        public abstract partial class Component<TComponent, TComponentBaseEventInterface>
            where TComponentBaseEventInterface : class, TAggregateRootBaseEventInterface
            where TComponent : Component<TComponent, TComponentBaseEventInterface>
        {
            [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
            public abstract class NestedComponent<TNestedComponent, TNestedComponentBaseEventInterface> : Component<TNestedComponent, TNestedComponentBaseEventInterface>
                where TNestedComponentBaseEventInterface : class, TComponentBaseEventInterface
                where TNestedComponent : NestedComponent<TNestedComponent, TNestedComponentBaseEventInterface>
            {
                protected NestedComponent(TComponent parent) : base(parent.RegisterEventAppliers(), registerEventAppliers: true) {}

                protected NestedComponent(IEventHandlerRegistrar<TNestedComponentBaseEventInterface> appliersRegistrar,
                                          bool registerEventAppliers) : base(appliersRegistrar, registerEventAppliers) {}
            }
        }
    }
}
