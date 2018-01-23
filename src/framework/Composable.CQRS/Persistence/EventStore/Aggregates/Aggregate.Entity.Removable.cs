using System;
using Composable.Messaging.Events;

namespace Composable.Persistence.EventStore.Aggregates
{
    public abstract partial class Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent>
        where TAggregate : Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent>
        where TAggregateEvent : class, IAggregateEvent
        where TAggregateEventImplementation : AggregateEvent, TAggregateEvent
    {
        public abstract class RemovableEntity<TEntity,
                                     TEntityId,
                                     TEntityEventImplementation,
                                     TEntityEvent,
                                     TEntityCreatedEvent,
                                     TEntityRemovedEvent,
                                     TEntityEventIdGetterSetter> : Entity<TEntity,
                                                                       TEntityId,
                                                                       TEntityEventImplementation,
                                                                       TEntityEvent,
                                                                       TEntityCreatedEvent,
                                                                       TEntityEventIdGetterSetter>
            where TEntityEvent : class, TAggregateEvent
            where TEntityEventImplementation : TAggregateEventImplementation, TEntityEvent
            where TEntityCreatedEvent : TEntityEvent
            where TEntityRemovedEvent : TEntityEvent
            where TEntity : RemovableEntity<TEntity,
                                TEntityId,
                                TEntityEventImplementation,
                                TEntityEvent,
                                TEntityCreatedEvent,
                                TEntityRemovedEvent,
                                TEntityEventIdGetterSetter>
            where TEntityEventIdGetterSetter : IGetSeTAggregateEntityEventEntityId<TEntityId, TEntityEventImplementation, TEntityEvent>,
                new()
        {
            static RemovableEntity() => AggregateTypeValidator<TEntity, TEntityEventImplementation, TEntityEvent>.AssertStaticStructureIsValid();

            protected RemovableEntity(TAggregate aggregate) : base(aggregate)
            {
                RegisterEventAppliers()
                    .IgnoreUnhandled<TEntityRemovedEvent>();
            }
            internal new static CollectionManager CreateSelfManagingCollection(TAggregate parent)
                =>
                    new CollectionManager(
                        parent: parent,
                        raiseEventThroughParent: parent.Publish,
                        appliersRegistrar: parent.RegisterEventAppliers());

            internal new class CollectionManager : EntityCollectionManager<TAggregate,
                                                     TEntity,
                                                     TEntityId,
                                                     TEntityEventImplementation,
                                                     TEntityEvent,
                                                     TEntityCreatedEvent,
                                                     TEntityRemovedEvent,
                                                     TEntityEventIdGetterSetter>
            {
                internal CollectionManager
                    (TAggregate parent,
                     Action<TEntityEventImplementation> raiseEventThroughParent,
                     IEventHandlerRegistrar<TEntityEvent> appliersRegistrar) : base(parent, raiseEventThroughParent, appliersRegistrar) {}
            }
        }
    }
}
