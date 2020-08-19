using System;
using Composable.DDD;
using Composable.Messaging;
using Composable.Messaging.Events;

namespace Composable.Persistence.EventStore.Aggregates
{
    public partial class Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent, TWrapperEventImplementation, TWrapperEventInterface>
        where TWrapperEventImplementation : TWrapperEventInterface
        where TWrapperEventInterface : IAggregateEvent<TAggregateEvent>
        where TAggregate : Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent, TWrapperEventImplementation, TWrapperEventInterface>
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
                where TEntityId : notnull
                where TEntityEvent : class, TAggregateEvent
                where TEntityCreatedEvent : TEntityEvent
                where TEntityRemovedEvent : TEntityEvent
                where TEntityEventImplementation : TEntityEvent, TAggregateEventImplementation
                where TEntity : Component<TEntity, TEntityEventImplementation, TEntityEvent>
                where TEntityEventIdGetterSetter :
                    IGetSetAggregateEntityEventEntityId<TEntityId, TEntityEventImplementation, TEntityEvent>
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
