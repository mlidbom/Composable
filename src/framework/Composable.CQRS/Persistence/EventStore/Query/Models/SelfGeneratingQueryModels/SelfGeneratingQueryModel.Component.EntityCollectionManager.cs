using Composable.Messaging.Events;
using Composable.Persistence.EventStore.Aggregates;
using Composable.System.Reflection;

namespace Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels
{
    public abstract partial class SelfGeneratingQueryModel<TAggregate, TAggregateBaseEventInterface>
        where TAggregate : SelfGeneratingQueryModel<TAggregate, TAggregateBaseEventInterface>
        where TAggregateBaseEventInterface : class, IAggregateRootEvent
    {
        public abstract partial class Component<TComponent, TComponentBaseEventInterface>
            where TComponentBaseEventInterface : class, TAggregateBaseEventInterface
            where TComponent : Component<TComponent, TComponentBaseEventInterface>
        {
            public class QueryModelEntityCollectionManager<TParent,
                                                 TEntity,
                                                 TEntityId,
                                                 TEntityBaseEventInterface,
                                                 TEntityCreatedEventInterface,
                                                 TEventEntityIdSetterGetter> : IQueryModelEntityCollectionManager<TEntity, TEntityId>
                where TEntityBaseEventInterface : class, TAggregateBaseEventInterface
                where TEntityCreatedEventInterface : TEntityBaseEventInterface
                where TEntity : Component<TEntity, TEntityBaseEventInterface>
                where TEventEntityIdSetterGetter : IGeTAggregateEntityEventEntityId<TEntityBaseEventInterface, TEntityId>, new()
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
