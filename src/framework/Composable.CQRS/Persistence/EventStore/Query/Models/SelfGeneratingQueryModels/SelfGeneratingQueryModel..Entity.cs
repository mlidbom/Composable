using Composable.Messaging.Events;
using Composable.Persistence.EventStore.Aggregates;
using JetBrains.Annotations;

namespace Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels
{
    public abstract partial class SelfGeneratingQueryModel<TAggregate, TAggregateEvent>
        where TAggregate : SelfGeneratingQueryModel<TAggregate, TAggregateEvent>
        where TAggregateEvent : class, IAggregateEvent
    {
        [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
        public abstract class Entity<TEntity,
                                     TEntityId,
                                     TEntityEvent,
                                     TEntityCreatedEvent,
                                     TEventEntityIdGetter> : Component<TEntity, TEntityEvent>
            where TEntityEvent : class, TAggregateEvent
            where TEntityCreatedEvent : TEntityEvent
            where TEntity : Entity<TEntity,
                                TEntityId,
                                TEntityEvent,
                                TEntityCreatedEvent,
                                TEventEntityIdGetter>
            where TEventEntityIdGetter : IGeTAggregateEntityEventEntityId<TEntityEvent, TEntityId>,
                new()
        {
            static readonly TEventEntityIdGetter IdGetter = new TEventEntityIdGetter();

            public TEntityId Id { get; private set; }

            protected Entity(TAggregate aggregateRoot) : this(aggregateRoot.RegisterEventAppliers()) {}

            Entity
                (IEventHandlerRegistrar<TEntityEvent> appliersRegistrar) : base(appliersRegistrar, registerEventAppliers: false)
            {
                RegisterEventAppliers()
                    .For<TEntityCreatedEvent>(e => Id = IdGetter.GetId(e));
            }

            public static CollectionManager CreateSelfManagingCollection(TAggregate parent) => new CollectionManager(parent, parent.RegisterEventAppliers());

            public class CollectionManager : QueryModelEntityCollectionManager<
                                                 TAggregate,
                                                 TEntity,
                                                 TEntityId,
                                                 TEntityEvent,
                                                 TEntityCreatedEvent,
                                                 TEventEntityIdGetter>
            {
                internal CollectionManager
                    (TAggregate parent, IEventHandlerRegistrar<TEntityEvent> appliersRegistrar) : base(parent, appliersRegistrar) {}
            }
        }
    }
}
