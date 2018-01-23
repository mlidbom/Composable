using Composable.Messaging.Events;
using JetBrains.Annotations;

namespace Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels
{
    public abstract partial class SelfGeneratingQueryModel<TQueryModel, TAggregateEvent>
        where TQueryModel : SelfGeneratingQueryModel<TQueryModel, TAggregateEvent>
        where TAggregateEvent : class, IAggregateEvent
    {
        public abstract partial class Component<TComponent, TComponentEvent>
            where TComponentEvent : class, TAggregateEvent
            where TComponent : Component<TComponent, TComponentEvent>
        {
            [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
            public abstract class NestedComponent<TNestedComponent, TNestedComponentEvent> : Component<TNestedComponent, TNestedComponentEvent>
                where TNestedComponentEvent : class, TComponentEvent
                where TNestedComponent : NestedComponent<TNestedComponent, TNestedComponentEvent>
            {
                protected NestedComponent(TComponent parent) : base(parent.RegisterEventAppliers(), registerEventAppliers: true) {}

                protected NestedComponent(IEventHandlerRegistrar<TNestedComponentEvent> appliersRegistrar,
                                          bool registerEventAppliers) : base(appliersRegistrar, registerEventAppliers) {}
            }
        }
    }
}
