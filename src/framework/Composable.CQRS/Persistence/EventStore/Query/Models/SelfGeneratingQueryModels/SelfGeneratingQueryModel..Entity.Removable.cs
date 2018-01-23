using Composable.Messaging.Events;
using Composable.Persistence.EventStore.Aggregates;

namespace Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels
{
    public abstract partial class SelfGeneratingQueryModel<TAggregate, TAggregateBaseEventInterface>
        where TAggregate : SelfGeneratingQueryModel<TAggregate, TAggregateBaseEventInterface>
        where TAggregateBaseEventInterface : class, IAggregateRootEvent
    {
        public abstract class Entity<TEntity,
                                     TEntityId,
                                     TEntityBaseEventInterface,
                                     TEntityCreatedEventInterface,
                                     TEntityRemovedEventInterface,
                                     TEventEntityIdGetter> : Entity<TEntity,
                                                                       TEntityId,
                                                                       TEntityBaseEventInterface,
                                                                       TEntityCreatedEventInterface,
                                                                       TEventEntityIdGetter>
            where TEntityBaseEventInterface : class, TAggregateBaseEventInterface
            where TEntityCreatedEventInterface : TEntityBaseEventInterface
            where TEntityRemovedEventInterface : TEntityBaseEventInterface
            where TEntity : Entity<TEntity,
                                TEntityId,
                                TEntityBaseEventInterface,
                                TEntityCreatedEventInterface,
                                TEntityRemovedEventInterface,
                                TEventEntityIdGetter>
            where TEventEntityIdGetter : IGeTAggregateEntityEventEntityId<TEntityBaseEventInterface, TEntityId>,
                new()
        {
            protected Entity(TAggregate aggregateRoot) : base(aggregateRoot)
            {
                RegisterEventAppliers()
                    .IgnoreUnhandled<TEntityRemovedEventInterface>();
            }
            internal new static CollectionManager CreateSelfManagingCollection(TAggregate parent) =>
                    new CollectionManager(parent: parent, appliersRegistrar: parent.RegisterEventAppliers());

            internal new class CollectionManager : QueryModelEntityCollectionManager<TAggregate,
                                                     TEntity,
                                                     TEntityId,
                                                     TEntityBaseEventInterface,
                                                     TEntityCreatedEventInterface,
                                                     TEntityRemovedEventInterface,
                                                     TEventEntityIdGetter>
            {
                internal CollectionManager(TAggregate parent, IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar): base(parent, appliersRegistrar) {}
            }
        }
    }
}
