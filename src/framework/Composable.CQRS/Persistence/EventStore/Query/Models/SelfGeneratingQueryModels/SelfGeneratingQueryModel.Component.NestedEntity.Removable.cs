using Composable.Messaging.Events;
using Composable.Persistence.EventStore.Aggregates;

namespace Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels
{
    public abstract partial class SelfGeneratingQueryModel<TAggregate, TAggregateBaseEventInterface>
        where TAggregate : SelfGeneratingQueryModel<TAggregate, TAggregateBaseEventInterface>
        where TAggregateBaseEventInterface : class, IAggregateRootEvent
    {
        public abstract partial class Component<TComponent, TComponentBaseEventInterface>
            where TComponentBaseEventInterface : class, TAggregateBaseEventInterface
            where TComponent : Component<TComponent, TComponentBaseEventInterface>
        {
            internal abstract class RemovableNestedEntity<TEntity,
                                               TEntityId,
                                               TEntityBaseEventInterface,
                                               TEntityCreatedEventInterface,
                                               TEntityRemovedEventInterface,
                                               TEventEntityIdGetter> :
                                                   NestedEntity<TEntity,
                                                       TEntityId,
                                                       TEntityBaseEventInterface,
                                                       TEntityCreatedEventInterface,
                                                       TEventEntityIdGetter>
                where TEntityBaseEventInterface : class, TComponentBaseEventInterface
                where TEntityCreatedEventInterface : TEntityBaseEventInterface
                where TEntityRemovedEventInterface : TEntityBaseEventInterface
                where TEventEntityIdGetter :
                    IGeTAggregateEntityEventEntityId<TEntityBaseEventInterface, TEntityId>, new()
                where TEntity : NestedEntity<TEntity,
                                    TEntityId,
                                    TEntityBaseEventInterface,
                                    TEntityCreatedEventInterface,
                                    TEventEntityIdGetter>
            {
                protected RemovableNestedEntity(TComponent parent) : this(parent.RegisterEventAppliers())
                {
                }

                RemovableNestedEntity(IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar): base(appliersRegistrar)
                {
                    RegisterEventAppliers()
                        .IgnoreUnhandled<TEntityRemovedEventInterface>();
                }

                internal new static CollectionManager CreateSelfManagingCollection(TComponent parent) =>
                        new CollectionManager(parent: parent, appliersRegistrar: parent.RegisterEventAppliers());

                internal new class CollectionManager : QueryModelEntityCollectionManager<TComponent,
                                                         TEntity,
                                                         TEntityId,
                                                         TEntityBaseEventInterface,
                                                         TEntityCreatedEventInterface,
                                                         TEntityRemovedEventInterface,
                                                         TEventEntityIdGetter>
                {
                    internal CollectionManager
                        (TComponent parent, IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar) : base(parent, appliersRegistrar) {}
                }
            }
        }
    }
}
