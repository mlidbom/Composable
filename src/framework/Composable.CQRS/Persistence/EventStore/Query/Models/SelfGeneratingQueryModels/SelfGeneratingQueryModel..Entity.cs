using Composable.GenericAbstractions.Time;
using Composable.Messaging.Events;
using Composable.Persistence.EventStore.AggregateRoots;
using JetBrains.Annotations;

namespace Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels
{
    public abstract partial class SelfGeneratingQueryModel<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>
        where TAggregateRoot : SelfGeneratingQueryModel<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>
        where TAggregateRootBaseEventInterface : class, IAggregateRootEvent
        where TAggregateRootBaseEventClass : AggregateRootEvent, TAggregateRootBaseEventInterface
    {
        [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
        public abstract class Entity<TEntity,
                                     TEntityId,
                                     TEntityBaseEventClass,
                                     TEntityBaseEventInterface,
                                     TEntityCreatedEventInterface,
                                     TEventEntityIdSetterGetter> : Component<TEntity, TEntityBaseEventClass, TEntityBaseEventInterface>
            where TEntityBaseEventInterface : class, TAggregateRootBaseEventInterface
            where TEntityBaseEventClass : TAggregateRootBaseEventClass, TEntityBaseEventInterface
            where TEntityCreatedEventInterface : TEntityBaseEventInterface
            where TEntity : Entity<TEntity,
                                TEntityId,
                                TEntityBaseEventClass,
                                TEntityBaseEventInterface,
                                TEntityCreatedEventInterface,
                                TEventEntityIdSetterGetter>
            where TEventEntityIdSetterGetter : IGetSetAggregateRootEntityEventEntityId<TEntityId, TEntityBaseEventClass, TEntityBaseEventInterface>,
                new()
        {
            static readonly TEventEntityIdSetterGetter IdGetterSetter = new TEventEntityIdSetterGetter();

            public TEntityId Id { get; private set; }

            protected Entity(TAggregateRoot aggregateRoot) : this(aggregateRoot.RegisterEventAppliers()) {}

            Entity
                (IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar) : base(appliersRegistrar, registerEventAppliers: false)
            {
                RegisterEventAppliers()
                    .For<TEntityCreatedEventInterface>(e => Id = IdGetterSetter.GetId(e));
            }

            // ReSharper disable once UnusedMember.Global todo: write tests.
            public static CollectionManager CreateSelfManagingCollection(TAggregateRoot parent)
                => new CollectionManager(parent, parent.RegisterEventAppliers());

            public class CollectionManager : QueryModelEntityCollectionManager<
                                                 TAggregateRoot,
                                                 TEntity,
                                                 TEntityId,
                                                 TEntityBaseEventClass,
                                                 TEntityBaseEventInterface,
                                                 TEntityCreatedEventInterface,
                                                 TEventEntityIdSetterGetter>
            {
                internal CollectionManager
                    (TAggregateRoot parent, IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar) : base(parent, appliersRegistrar) {}
            }
        }
    }
}
