using System;
using Composable.Contracts;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Events;
using Composable.SystemCE.Reflection;
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
            where TEntityEventIdGetterSetter : IGetSetAggregateEntityEventEntityId<TEntityId, TEntityEventImplementation, TEntityEvent>
        {
            static Entity() => AggregateTypeValidator<TEntity, TEntityEventImplementation, TEntityEvent>.AssertStaticStructureIsValid();

            static readonly TEntityEventIdGetterSetter IdGetterSetter = Constructor.For<TEntityEventIdGetterSetter>.DefaultConstructor.Instance();

            TEntityId _id;
            public TEntityId Id => Assert.Result.NotNullOrDefault(_id);

            protected Entity(TAggregate aggregate)
                : this(aggregate.TimeSource, @event => aggregate.Publish(@event), aggregate.RegisterEventAppliers()) {}

#pragma warning disable CS8618 //Review OK-ish: We ensure that we never return a null or default value from the public Id property
            Entity
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
                (IUtcTimeTimeSource timeSource,
                 Action<TEntityEventImplementation> raiseEventThroughParent,
                 IEventHandlerRegistrar<TEntityEvent> appliersRegistrar)
                : base(timeSource, raiseEventThroughParent, appliersRegistrar, registerEventAppliers: false)
            {
                RegisterEventAppliers()
                    .For<TEntityCreatedEvent>(e => _id = IdGetterSetter.GetId(e));
            }

            protected override void Publish(TEntityEventImplementation @event)
            {
                var id = IdGetterSetter.GetId(@event);
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if(Equals(id, default(TEntityId)!))
                    // ReSharper disable HeuristicUnreachableCode
                {
                    IdGetterSetter.SetEntityId(@event, Id);
                }
                // ReSharper restore HeuristicUnreachableCode
                else if(!Equals(id, Id))
                {
                    throw new Exception($"Attempted to raise event with EntityId: {id} frow within entity with EntityId: {Id}");
                }
                base.Publish(@event);
            }

            // ReSharper disable once UnusedMember.Global todo: write tests.
            public static CollectionManager CreateSelfManagingCollection(TAggregate parent)
                => new CollectionManager(parent, @event => parent.Publish(@event), parent.RegisterEventAppliers());

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
