using System;
using Composable.Messaging.Events;

namespace Composable.Persistence.EventStore.Aggregates
{
    public abstract partial class Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent>
        where TAggregate : Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent>
        where TAggregateEvent : class, IAggregateEvent
        where TAggregateEventImplementation : AggregateEvent, TAggregateEvent
    {
        public abstract partial class Component<TComponent, TComponentEventImplementation, TComponentEvent>
            where TComponentEvent : class, TAggregateEvent
            where TComponentEventImplementation : TAggregateEventImplementation, TComponentEvent
            where TComponent : Component<TComponent, TComponentEventImplementation, TComponentEvent>
        {
            internal class EntityCollectionManager<TParent,
                                                 TEntity,
                                                 TEntityId,
                                                 TEntityEventImplementation,
                                                 TEntityEvent,
                                                 TEntityCreatedEvent,
                                                 TEntityRemovedEvent,
                                                 TEntityEventIdGetterSetter> : EntityCollectionManager<TParent,
                                                                                   TEntity,
                                                                                   TEntityId,
                                                                                   TEntityEventImplementation,
                                                                                   TEntityEvent,
                                                                                   TEntityCreatedEvent,
                                                                                   TEntityEventIdGetterSetter>
                where TEntityEvent : class, TAggregateEvent
                where TEntityCreatedEvent : TEntityEvent
                where TEntityRemovedEvent : TEntityEvent
                where TEntityEventImplementation : TEntityEvent, TAggregateEventImplementation
                where TEntity : Component<TEntity, TEntityEventImplementation, TEntityEvent>
                where TEntityEventIdGetterSetter :
                    IGetSeTAggregateEntityEventEntityId<TEntityId, TEntityEventImplementation, TEntityEvent>, new()
            {
                protected EntityCollectionManager
                    (TParent parent,
                     Action<TEntityEventImplementation> raiseEventThroughParent,
                     IEventHandlerRegistrar<TEntityEvent> appliersRegistrar)
                    : base(parent, raiseEventThroughParent, appliersRegistrar)
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
