using Composable.Messaging.Events;
using Composable.Persistence.EventStore.AggregateRoots;
using Composable.System.Reflection;

namespace Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels
{
    public abstract partial class SelfGeneratingQueryModel<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>
        where TAggregateRoot : SelfGeneratingQueryModel<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>
        where TAggregateRootBaseEventInterface : class, IAggregateRootEvent
        where TAggregateRootBaseEventClass : AggregateRootEvent, TAggregateRootBaseEventInterface
    {
        public abstract partial class Component<TComponent, TComponentBaseEventClass, TComponentBaseEventInterface>
            where TComponentBaseEventInterface : class, TAggregateRootBaseEventInterface
            where TComponentBaseEventClass : TAggregateRootBaseEventClass, TComponentBaseEventInterface
            where TComponent : Component<TComponent, TComponentBaseEventClass, TComponentBaseEventInterface>
        {
            public class QueryModelEntityCollectionManager<TParent,
                                                 TEntity,
                                                 TEntityId,
                                                 TEntityBaseEventClass,
                                                 TEntityBaseEventInterface,
                                                 TEntityCreatedEventInterface,
                                                 TEventEntityIdSetterGetter> : IQueryModelEntityCollectionManager<TEntity, TEntityId>
                where TEntityBaseEventInterface : class, TAggregateRootBaseEventInterface
                where TEntityCreatedEventInterface : TEntityBaseEventInterface
                where TEntityBaseEventClass : TEntityBaseEventInterface, TAggregateRootBaseEventClass
                where TEntity : Component<TEntity, TEntityBaseEventClass, TEntityBaseEventInterface>
                where TEventEntityIdSetterGetter : IGetAggregateRootEntityEventEntityId<TEntityBaseEventInterface, TEntityId>, new()
            {
                protected static readonly TEventEntityIdSetterGetter IdGetter = new TEventEntityIdSetterGetter();

                protected readonly QueryModelEntityCollection<TEntity, TEntityId> ManagedEntities;
                protected QueryModelEntityCollectionManager(TParent parent, IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar)
                {
                    ManagedEntities = new QueryModelEntityCollection<TEntity, TEntityId>();
                    appliersRegistrar
                        .For<TEntityCreatedEventInterface>(
                            e =>
                            {
                                var entity = ObjectFactory<TEntity>.CreateInstance(parent);
                                ManagedEntities.Add(entity, IdGetter.GetId(e));
                            })
                        .For<TEntityBaseEventInterface>(e => ManagedEntities[IdGetter.GetId(e)].ApplyEvent(e));
                }

                public IReadonlyQueryModelEntityCollection<TEntity, TEntityId> Entities => ManagedEntities;
            }
        }
    }
}
