using Composable.Messaging.Events;
using Composable.Persistence.EventStore.AggregateRoots;

namespace Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels
{
    public abstract partial class SelfGeneratingQueryModel<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>
        where TAggregateRoot : SelfGeneratingQueryModel<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>
        where TAggregateRootBaseEventInterface : class, IAggregateRootEvent
        where TAggregateRootBaseEventClass : AggregateRootEvent, TAggregateRootBaseEventInterface
    {
        public abstract class Entity<TEntity,
                                     TEntityId,
                                     TEntityBaseEventClass,
                                     TEntityBaseEventInterface,
                                     TEntityCreatedEventInterface,
                                     TEntityRemovedEventInterface,
                                     TEventEntityIdSetterGetter> : Entity<TEntity,
                                                                       TEntityId,
                                                                       TEntityBaseEventClass,
                                                                       TEntityBaseEventInterface,
                                                                       TEntityCreatedEventInterface,
                                                                       TEventEntityIdSetterGetter>
            where TEntityBaseEventInterface : class, TAggregateRootBaseEventInterface
            where TEntityBaseEventClass : TAggregateRootBaseEventClass, TEntityBaseEventInterface
            where TEntityCreatedEventInterface : TEntityBaseEventInterface
            where TEntityRemovedEventInterface : TEntityBaseEventInterface
            where TEntity : Entity<TEntity,
                                TEntityId,
                                TEntityBaseEventClass,
                                TEntityBaseEventInterface,
                                TEntityCreatedEventInterface,
                                TEntityRemovedEventInterface,
                                TEventEntityIdSetterGetter>
            where TEventEntityIdSetterGetter : IGetSetAggregateRootEntityEventEntityId<TEntityId, TEntityBaseEventClass, TEntityBaseEventInterface>,
                new()
        {
            protected Entity(TAggregateRoot aggregateRoot) : base(aggregateRoot)
            {
                RegisterEventAppliers()
                    .IgnoreUnhandled<TEntityRemovedEventInterface>();
            }
            internal new static CollectionManager CreateSelfManagingCollection(TAggregateRoot parent)
                =>
                    new CollectionManager(
                        parent: parent,
                        appliersRegistrar: parent.RegisterEventAppliers());

            internal new class CollectionManager : EntityCollectionManager<TAggregateRoot,
                                                     TEntity,
                                                     TEntityId,
                                                     TEntityBaseEventClass,
                                                     TEntityBaseEventInterface,
                                                     TEntityCreatedEventInterface,
                                                     TEntityRemovedEventInterface,
                                                     TEventEntityIdSetterGetter>
            {
                internal CollectionManager(TAggregateRoot parent, IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar): base(parent, appliersRegistrar) {}
            }
        }
    }
}
