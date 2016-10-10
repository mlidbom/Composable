using System;
using Composable.CQRS.EventHandling;
using Composable.GenericAbstractions.Time;

namespace Composable.CQRS.EventSourcing
{
    public abstract partial class AggregateRoot<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>
        where TAggregateRoot : AggregateRoot<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>
        where TAggregateRootBaseEventInterface : class, IAggregateRootEvent
        where TAggregateRootBaseEventClass : AggregateRootEvent, TAggregateRootBaseEventInterface
    {
        public abstract class Entity<TEntity,
                                     TEntityId,
                                     TEntityBaseEventClass,
                                     TEntityBaseEventInterface,
                                     TEntityCreatedEventInterface,
                                     TEventEntityIdSetterGetter> : Component<TEntity, TEntityBaseEventClass, TEntityBaseEventInterface>
            where TEntityBaseEventInterface : class, TAggregateRootBaseEventInterface
            where TEntityBaseEventClass : TAggregateRootBaseEventClass, TEntityBaseEventInterface
            where TEntityCreatedEventInterface : TEntityBaseEventInterface
            where TEntity : Entity<TEntity,
                                TEntityId,
                                TEntityBaseEventClass,
                                TEntityBaseEventInterface,
                                TEntityCreatedEventInterface,
                                TEventEntityIdSetterGetter>
            where TEventEntityIdSetterGetter : IGetSetAggregateRootEntityEventEntityId<TEntityId, TEntityBaseEventClass, TEntityBaseEventInterface>,
                new()
        {
            private static readonly TEventEntityIdSetterGetter IdGetterSetter = new TEventEntityIdSetterGetter();

            public TEntityId Id { get; private set; }

            protected Entity(TAggregateRoot aggregateRoot)
                : this(aggregateRoot.TimeSource, aggregateRoot.RaiseEvent, aggregateRoot.RegisterEventAppliers()) {}

            protected Entity
                (IUtcTimeTimeSource timeSource,
                 Action<TEntityBaseEventClass> raiseEventThroughParent,
                 IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar)
                : base(timeSource, raiseEventThroughParent, appliersRegistrar, registerEventAppliers: false)
            {
                RegisterEventAppliers()
                    .For<TEntityCreatedEventInterface>(e => Id = IdGetterSetter.GetId(e));
            }

            protected override void RaiseEvent(TEntityBaseEventClass @event)
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
                base.RaiseEvent(@event);
            }

            public static CollectionManager CreateSelfManagingCollection(TAggregateRoot parent)
                => new CollectionManager(parent, parent.RaiseEvent, parent.RegisterEventAppliers());

            public class CollectionManager : EntityCollectionManager<
                                                 TAggregateRoot,
                                                 TEntity,
                                                 TEntityId,
                                                 TEntityBaseEventClass,
                                                 TEntityBaseEventInterface,
                                                 TEntityCreatedEventInterface,
                                                 TEventEntityIdSetterGetter>
            {
                public CollectionManager
                    (TAggregateRoot parent,
                     Action<TEntityBaseEventClass> raiseEventThroughParent,
                     IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar) : base(parent, raiseEventThroughParent, appliersRegistrar) {}
            }
        }
    }
}
