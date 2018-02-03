using Composable.Messaging.Events;
using Composable.Persistence.EventStore.Aggregates;

namespace Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels
{
    public abstract partial class SelfGeneratingQueryModel<TQueryModel, TAggregateEvent>
        where TQueryModel : SelfGeneratingQueryModel<TQueryModel, TAggregateEvent>
        where TAggregateEvent : class, IAggregateEvent
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
            where TEventEntityIdGetter : IGetAggregateEntityEventEntityId<TEntityEvent, TEntityId>
        {
            protected Entity(TQueryModel queryModel) : base(queryModel)
            {
                RegisterEventAppliers()
                    .IgnoreUnhandled<TEntityRemovedEvent>();
            }
            internal new static CollectionManager CreateSelfManagingCollection(TQueryModel parent) =>
                    new CollectionManager(parent: parent, appliersRegistrar: parent.RegisterEventAppliers());

            internal new class CollectionManager : QueryModelEntityCollectionManager<TQueryModel,
                                                     TEntity,
                                                     TEntityId,
                                                     TEntityEvent,
                                                     TEntityCreatedEvent,
                                                     TEntityRemovedEvent,
                                                     TEventEntityIdGetter>
            {
                internal CollectionManager(TQueryModel parent, IEventHandlerRegistrar<TEntityEvent> appliersRegistrar): base(parent, appliersRegistrar) {}
            }
        }
    }
}
