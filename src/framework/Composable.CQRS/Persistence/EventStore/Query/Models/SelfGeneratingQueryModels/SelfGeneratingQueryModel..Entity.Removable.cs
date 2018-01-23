using Composable.Messaging.Events;
using Composable.Persistence.EventStore.Aggregates;

namespace Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels
{
    public abstract partial class SelfGeneratingQueryModel<TAggregate, TAggregateEvent>
        where TAggregate : SelfGeneratingQueryModel<TAggregate, TAggregateEvent>
        where TAggregateEvent : class, IAggregateRootEvent
    {
        public abstract class Entity<TEntity,
                                     TEntityId,
                                     TEntityEvent,
                                     TEntityCreatedEvent,
                                     TEntityRemovedEvent,
                                     TEventEntityIdGetter> : Entity<TEntity,
                                                                       TEntityId,
                                                                       TEntityEvent,
                                                                       TEntityCreatedEvent,
                                                                       TEventEntityIdGetter>
            where TEntityEvent : class, TAggregateEvent
            where TEntityCreatedEvent : TEntityEvent
            where TEntityRemovedEvent : TEntityEvent
            where TEntity : Entity<TEntity,
                                TEntityId,
                                TEntityEvent,
                                TEntityCreatedEvent,
                                TEntityRemovedEvent,
                                TEventEntityIdGetter>
            where TEventEntityIdGetter : IGeTAggregateEntityEventEntityId<TEntityEvent, TEntityId>,
                new()
        {
            protected Entity(TAggregate aggregateRoot) : base(aggregateRoot)
            {
                RegisterEventAppliers()
                    .IgnoreUnhandled<TEntityRemovedEvent>();
            }
            internal new static CollectionManager CreateSelfManagingCollection(TAggregate parent) =>
                    new CollectionManager(parent: parent, appliersRegistrar: parent.RegisterEventAppliers());

            internal new class CollectionManager : QueryModelEntityCollectionManager<TAggregate,
                                                     TEntity,
                                                     TEntityId,
                                                     TEntityEvent,
                                                     TEntityCreatedEvent,
                                                     TEntityRemovedEvent,
                                                     TEventEntityIdGetter>
            {
                internal CollectionManager(TAggregate parent, IEventHandlerRegistrar<TEntityEvent> appliersRegistrar): base(parent, appliersRegistrar) {}
            }
        }
    }
}
