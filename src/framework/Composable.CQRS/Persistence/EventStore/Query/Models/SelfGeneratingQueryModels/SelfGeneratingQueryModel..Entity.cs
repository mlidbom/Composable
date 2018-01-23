using Composable.Messaging.Events;
using Composable.Persistence.EventStore.AggregateRoots;
using JetBrains.Annotations;

namespace Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels
{
    public abstract partial class SelfGeneratingQueryModel<TAggregateRoot, TAggregateRootBaseEventInterface>
        where TAggregateRoot : SelfGeneratingQueryModel<TAggregateRoot, TAggregateRootBaseEventInterface>
        where TAggregateRootBaseEventInterface : class, IAggregateRootEvent
    {
        [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
        public abstract class Entity<TEntity,
                                     TEntityId,
                                     TEntityBaseEventInterface,
                                     TEntityCreatedEventInterface,
                                     TEventEntityIdGetter> : Component<TEntity, TEntityBaseEventInterface>
            where TEntityBaseEventInterface : class, TAggregateRootBaseEventInterface
            where TEntityCreatedEventInterface : TEntityBaseEventInterface
            where TEntity : Entity<TEntity,
                                TEntityId,
                                TEntityBaseEventInterface,
                                TEntityCreatedEventInterface,
                                TEventEntityIdGetter>
            where TEventEntityIdGetter : IGetAggregateRootEntityEventEntityId<TEntityBaseEventInterface, TEntityId>,
                new()
        {
            static readonly TEventEntityIdGetter IdGetter = new TEventEntityIdGetter();

            public TEntityId Id { get; private set; }

            protected Entity(TAggregateRoot aggregateRoot) : this(aggregateRoot.RegisterEventAppliers()) {}

            Entity
                (IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar) : base(appliersRegistrar, registerEventAppliers: false)
            {
                RegisterEventAppliers()
                    .For<TEntityCreatedEventInterface>(e => Id = IdGetter.GetId(e));
            }

            public static CollectionManager CreateSelfManagingCollection(TAggregateRoot parent) => new CollectionManager(parent, parent.RegisterEventAppliers());

            public class CollectionManager : QueryModelEntityCollectionManager<
                                                 TAggregateRoot,
                                                 TEntity,
                                                 TEntityId,
                                                 TEntityBaseEventInterface,
                                                 TEntityCreatedEventInterface,
                                                 TEventEntityIdGetter>
            {
                internal CollectionManager
                    (TAggregateRoot parent, IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar) : base(parent, appliersRegistrar) {}
            }
        }
    }
}
