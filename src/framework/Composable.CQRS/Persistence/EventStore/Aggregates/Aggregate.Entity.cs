using System;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Events;
using JetBrains.Annotations;

namespace Composable.Persistence.EventStore.Aggregates
{
    public abstract partial class Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent>
        where TAggregate : Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent>
        where TAggregateEvent : class, IAggregateEvent
        where TAggregateEventImplementation : AggregateEvent, TAggregateEvent
    {
        [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
        public abstract class Entity<TEntity,
                                     TEntityId,
                                     TEntityEventImplementation,
                                     TEntityEvent,
                                     TEntityCreatedEvent,
                                     TEntityEventIdGetterSetter> : Component<TEntity, TEntityEventImplementation, TEntityEvent>
            where TEntityEvent : class, TAggregateEvent
            where TEntityEventImplementation : TAggregateEventImplementation, TEntityEvent
            where TEntityCreatedEvent : TEntityEvent
            where TEntity : Entity<TEntity,
                                TEntityId,
                                TEntityEventImplementation,
                                TEntityEvent,
                                TEntityCreatedEvent,
                                TEntityEventIdGetterSetter>
            where TEntityEventIdGetterSetter : IGetSeTAggregateEntityEventEntityId<TEntityId, TEntityEventImplementation, TEntityEvent>,
                new()
        {
            static Entity() => AggregateTypeValidator<TEntity, TEntityEventImplementation, TEntityEvent>.AssertStaticStructureIsValid();

            static readonly TEntityEventIdGetterSetter IdGetterSetter = new TEntityEventIdGetterSetter();

            public TEntityId Id { get; private set; }

            protected Entity(TAggregate aggregate)
                : this(aggregate.TimeSource, aggregate.Publish, aggregate.RegisterEventAppliers()) {}

            Entity
                (IUtcTimeTimeSource timeSource,
                 Action<TEntityEventImplementation> raiseEventThroughParent,
                 IEventHandlerRegistrar<TEntityEvent> appliersRegistrar)
                : base(timeSource, raiseEventThroughParent, appliersRegistrar, registerEventAppliers: false)
            {
                RegisterEventAppliers()
                    .For<TEntityCreatedEvent>(e => Id = IdGetterSetter.GetId(e));
            }

            protected override void Publish(TEntityEventImplementation @event)
            {
                var id = IdGetterSetter.GetId(@event);
                if(Equals(id, default(TEntityId)))
                {
                    IdGetterSetter.SetEntityId(@event, Id);
                }
                else if(!Equals(id, Id))
                {
                    throw new Exception($"Attempted to raise event with EntityId: {id} frow within entity with EntityId: {Id}");
                }
                base.Publish(@event);
            }

            // ReSharper disable once UnusedMember.Global todo: write tests.
            public static CollectionManager CreateSelfManagingCollection(TAggregate parent)
                => new CollectionManager(parent, parent.Publish, parent.RegisterEventAppliers());

            public class CollectionManager : EntityCollectionManager<
                                                 TAggregate,
                                                 TEntity,
                                                 TEntityId,
                                                 TEntityEventImplementation,
                                                 TEntityEvent,
                                                 TEntityCreatedEvent,
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
