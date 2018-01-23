using Composable.Messaging.Events;
using Composable.Persistence.EventStore.Aggregates;

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
            internal class QueryModelEntityCollectionManager<TParent,
                                                 TEntity,
                                                 TEntityId,
                                                 TEntityBaseEventInterface,
                                                 TEntityCreatedEventInterface,
                                                 TEntityRemovedEventInterface,
                                                 TEventEntityIdGetter> : QueryModelEntityCollectionManager<TParent,
                                                                                   TEntity,
                                                                                   TEntityId,
                                                                                   TEntityBaseEventInterface,
                                                                                   TEntityCreatedEventInterface,
                                                                                   TEventEntityIdGetter>
                where TEntityBaseEventInterface : class, TAggregateBaseEventInterface
                where TEntityCreatedEventInterface : TEntityBaseEventInterface
                where TEntityRemovedEventInterface : TEntityBaseEventInterface
                where TEntity : Component<TEntity, TEntityBaseEventInterface>
                where TEventEntityIdGetter : IGeTAggregateEntityEventEntityId<TEntityBaseEventInterface, TEntityId>, new()
            {
                protected QueryModelEntityCollectionManager (TParent parent, IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar) : base(parent, appliersRegistrar)
                {
                    appliersRegistrar.For<TEntityRemovedEventInterface>(
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
