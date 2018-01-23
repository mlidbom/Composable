using Composable.Messaging.Events;
using Composable.Persistence.EventStore.Aggregates;

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
                    IGetAggregateRootEntityEventEntityId<TEntityBaseEventInterface, TEntityId>, new()
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
