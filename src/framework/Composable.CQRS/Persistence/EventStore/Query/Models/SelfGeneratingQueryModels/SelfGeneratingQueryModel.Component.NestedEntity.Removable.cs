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
            internal abstract class RemovableNestedEntity<TEntity,
                                               TEntityId,
                                               TEntityEvent,
                                               TEntityCreatedEvent,
                                               TEntityRemovedEvent,
                                               TEventEntityIdGetter> :
                                                   NestedEntity<TEntity,
                                                       TEntityId,
                                                       TEntityEvent,
                                                       TEntityCreatedEvent,
                                                       TEventEntityIdGetter>
                where TEntityEvent : class, TComponentEvent
                where TEntityCreatedEvent : TEntityEvent
                where TEntityRemovedEvent : TEntityEvent
                where TEventEntityIdGetter :
                    IGetAggregateEntityEventEntityId<TEntityEvent, TEntityId>
                where TEntity : NestedEntity<TEntity,
                                    TEntityId,
                                    TEntityEvent,
                                    TEntityCreatedEvent,
                                    TEventEntityIdGetter>
            {
                protected RemovableNestedEntity(TComponent parent) : this(parent.RegisterEventAppliers())
                {
                }

                RemovableNestedEntity(IEventHandlerRegistrar<TEntityEvent> appliersRegistrar): base(appliersRegistrar)
                {
                    RegisterEventAppliers()
                        .IgnoreUnhandled<TEntityRemovedEvent>();
                }

                internal new static CollectionManager CreateSelfManagingCollection(TComponent parent) =>
                        new CollectionManager(parent: parent, appliersRegistrar: parent.RegisterEventAppliers());

                internal new class CollectionManager : QueryModelEntityCollectionManager<TComponent,
                                                         TEntity,
                                                         TEntityId,
                                                         TEntityEvent,
                                                         TEntityCreatedEvent,
                                                         TEntityRemovedEvent,
                                                         TEventEntityIdGetter>
                {
                    internal CollectionManager(TComponent parent, IEventHandlerRegistrar<TEntityEvent> appliersRegistrar) : base(parent, appliersRegistrar) {}
                }
            }
        }
    }
}
