using Composable.Messaging.Events;
using Composable.Persistence.EventStore.Aggregates;

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
            internal class QueryModelEntityCollectionManager<TParent,
                                                 TEntity,
                                                 TEntityId,
                                                 TEntityEvent,
                                                 TEntityCreatedEvent,
                                                 TEntityRemovedEvent,
                                                 TEventEntityIdGetter> : QueryModelEntityCollectionManager<TParent,
                                                                                   TEntity,
                                                                                   TEntityId,
                                                                                   TEntityEvent,
                                                                                   TEntityCreatedEvent,
                                                                                   TEventEntityIdGetter>
                where TEntityId : notnull
                where TEntityEvent : class, TAggregateEvent
                where TEntityCreatedEvent : TEntityEvent
                where TEntityRemovedEvent : TEntityEvent
                where TEntity : Component<TEntity, TEntityEvent>
                where TEventEntityIdGetter : IGetAggregateEntityEventEntityId<TEntityEvent, TEntityId>
            {
                protected QueryModelEntityCollectionManager (TParent parent, IEventHandlerRegistrar<TEntityEvent> appliersRegistrar) : base(parent, appliersRegistrar)
                {
                    appliersRegistrar.For<TEntityRemovedEvent>(
                        e =>
                        {
                            var id = IdGetter.GetId(e);
                            ManagedEntities.Remove(id);
                        });
                }
            }
        }
    }
}
