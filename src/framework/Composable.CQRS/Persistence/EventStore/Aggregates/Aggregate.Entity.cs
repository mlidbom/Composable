using System;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Events;
using JetBrains.Annotations;

namespace Composable.Persistence.EventStore.Aggregates
{
    public abstract partial class Aggregate<TAggregate, TAggregateBaseEventClass, TAggregateBaseEventInterface>
        where TAggregate : Aggregate<TAggregate, TAggregateBaseEventClass, TAggregateBaseEventInterface>
        where TAggregateBaseEventInterface : class, IAggregateRootEvent
        where TAggregateBaseEventClass : AggregateRootEvent, TAggregateBaseEventInterface
    {
        [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
        public abstract class Entity<TEntity,
                                     TEntityId,
                                     TEntityBaseEventClass,
                                     TEntityBaseEventInterface,
                                     TEntityCreatedEventInterface,
                                     TEventEntityIdSetterGetter> : Component<TEntity, TEntityBaseEventClass, TEntityBaseEventInterface>
            where TEntityBaseEventInterface : class, TAggregateBaseEventInterface
            where TEntityBaseEventClass : TAggregateBaseEventClass, TEntityBaseEventInterface
            where TEntityCreatedEventInterface : TEntityBaseEventInterface
            where TEntity : Entity<TEntity,
                                TEntityId,
                                TEntityBaseEventClass,
                                TEntityBaseEventInterface,
                                TEntityCreatedEventInterface,
                                TEventEntityIdSetterGetter>
            where TEventEntityIdSetterGetter : IGetSeTAggregateEntityEventEntityId<TEntityId, TEntityBaseEventClass, TEntityBaseEventInterface>,
                new()
        {
            static Entity() => AggregateTypeValidator<TEntity, TEntityBaseEventClass, TEntityBaseEventInterface>.AssertStaticStructureIsValid();

            static readonly TEventEntityIdSetterGetter IdGetterSetter = new TEventEntityIdSetterGetter();

            public TEntityId Id { get; private set; }

            protected Entity(TAggregate aggregateRoot)
                : this(aggregateRoot.TimeSource, aggregateRoot.Publish, aggregateRoot.RegisterEventAppliers()) {}

            Entity
                (IUtcTimeTimeSource timeSource,
                 Action<TEntityBaseEventClass> raiseEventThroughParent,
                 IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar)
                : base(timeSource, raiseEventThroughParent, appliersRegistrar, registerEventAppliers: false)
            {
                RegisterEventAppliers()
                    .For<TEntityCreatedEventInterface>(e => Id = IdGetterSetter.GetId(e));
            }

            protected override void Publish(TEntityBaseEventClass @event)
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
                                                 TEntityBaseEventClass,
                                                 TEntityBaseEventInterface,
                                                 TEntityCreatedEventInterface,
                                                 TEventEntityIdSetterGetter>
            {
                internal CollectionManager
                    (TAggregate parent,
                     Action<TEntityBaseEventClass> raiseEventThroughParent,
                     IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar) : base(parent, raiseEventThroughParent, appliersRegistrar) {}
            }
        }
    }
}
