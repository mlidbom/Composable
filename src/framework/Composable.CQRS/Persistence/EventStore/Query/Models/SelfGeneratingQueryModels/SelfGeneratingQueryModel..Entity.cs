using Composable.Messaging.Events;
using Composable.Persistence.EventStore.Aggregates;
using JetBrains.Annotations;

namespace Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels
{
    public abstract partial class SelfGeneratingQueryModel<TAggregate, TAggregateBaseEventInterface>
        where TAggregate : SelfGeneratingQueryModel<TAggregate, TAggregateBaseEventInterface>
        where TAggregateBaseEventInterface : class, IAggregateRootEvent
    {
        [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
        public abstract class Entity<TEntity,
                                     TEntityId,
                                     TEntityBaseEventInterface,
                                     TEntityCreatedEventInterface,
                                     TEventEntityIdGetter> : Component<TEntity, TEntityBaseEventInterface>
            where TEntityBaseEventInterface : class, TAggregateBaseEventInterface
            where TEntityCreatedEventInterface : TEntityBaseEventInterface
            where TEntity : Entity<TEntity,
                                TEntityId,
                                TEntityBaseEventInterface,
                                TEntityCreatedEventInterface,
                                TEventEntityIdGetter>
            where TEventEntityIdGetter : IGeTAggregateEntityEventEntityId<TEntityBaseEventInterface, TEntityId>,
                new()
        {
            static readonly TEventEntityIdGetter IdGetter = new TEventEntityIdGetter();

            public TEntityId Id { get; private set; }

            protected Entity(TAggregate aggregateRoot) : this(aggregateRoot.RegisterEventAppliers()) {}

            Entity
                (IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar) : base(appliersRegistrar, registerEventAppliers: false)
            {
                RegisterEventAppliers()
                    .For<TEntityCreatedEventInterface>(e => Id = IdGetter.GetId(e));
            }

            public static CollectionManager CreateSelfManagingCollection(TAggregate parent) => new CollectionManager(parent, parent.RegisterEventAppliers());

            public class CollectionManager : QueryModelEntityCollectionManager<
                                                 TAggregate,
                                                 TEntity,
                                                 TEntityId,
                                                 TEntityBaseEventInterface,
                                                 TEntityCreatedEventInterface,
                                                 TEventEntityIdGetter>
            {
                internal CollectionManager
                    (TAggregate parent, IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar) : base(parent, appliersRegistrar) {}
            }
        }
    }
}
