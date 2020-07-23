using Composable.Messaging.Events;
using Composable.Persistence.EventStore.Aggregates;
using Composable.SystemCE.ReflectionCE;

namespace Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels
{
    public abstract partial class SelfGeneratingQueryModel<TQueryModel, TAggregateEvent>
        where TQueryModel : SelfGeneratingQueryModel<TQueryModel, TAggregateEvent>
        where TAggregateEvent : class, IAggregateEvent
    {
        public abstract partial class Component<TComponent, TComponentEvent>
            where TComponentEvent : class, TAggregateEvent
            where TComponent : Component<TComponent, TComponentEvent>
        {
            public class QueryModelEntityCollectionManager<TParent,
                                                 TEntity,
                                                 TEntityId,
                                                 TEntityEvent,
                                                 TEntityCreatedEvent,
                                                 TEntityEventIdGetterSetter> : IQueryModelEntityCollectionManager<TEntity, TEntityId>
                where TEntityEvent : class, TAggregateEvent
                where TEntityCreatedEvent : TEntityEvent
                where TEntity : Component<TEntity, TEntityEvent>
                where TEntityEventIdGetterSetter : IGetAggregateEntityEventEntityId<TEntityEvent, TEntityId>
            {
                protected static readonly TEntityEventIdGetterSetter IdGetter = Constructor.For<TEntityEventIdGetterSetter>.DefaultConstructor.Instance();

                protected readonly QueryModelEntityCollection<TEntity, TEntityId> ManagedEntities;
                protected QueryModelEntityCollectionManager(TParent parent, IEventHandlerRegistrar<TEntityEvent> appliersRegistrar)
                {
                    ManagedEntities = new QueryModelEntityCollection<TEntity, TEntityId>();
                    appliersRegistrar
                        .For<TEntityCreatedEvent>(
                            e =>
                            {
                                var entity = Constructor.For<TEntity>.WithArgument<TParent>.Instance(parent);
                                ManagedEntities.Add(entity, IdGetter.GetId(e));
                            })
                        .For<TEntityEvent>(e => ManagedEntities[IdGetter.GetId(e)].ApplyEvent(e));
                }

                public IReadonlyQueryModelEntityCollection<TEntity, TEntityId> Entities => ManagedEntities;
            }
        }
    }
}
