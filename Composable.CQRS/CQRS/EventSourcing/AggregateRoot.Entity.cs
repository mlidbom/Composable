using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Composable.System.Reflection;

namespace Composable.CQRS.EventSourcing
{
    public abstract partial class AggregateRoot<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>
    {
        public abstract class Entity<TEntity,
                                     TEntityBaseEventClass,
                                     TEntityBaseEventInterface,
                                     TEntityCreatedEventInterface,
                                     TEntityRemovedEventInterface,
                                     TEventEntityIdSetterGetter> : Entity<TEntity,
                                                                       TEntityBaseEventClass,
                                                                       TEntityBaseEventInterface,
                                                                       TEntityCreatedEventInterface,
                                                                       TEventEntityIdSetterGetter>
            where TEntityBaseEventInterface : TAggregateRootBaseEventInterface
            where TEntityBaseEventClass : TAggregateRootBaseEventClass, TEntityBaseEventInterface
            where TEntityCreatedEventInterface : TEntityBaseEventInterface
            where TEntityRemovedEventInterface : TEntityBaseEventInterface
            where TEntity : Entity<TEntity,
                                TEntityBaseEventClass,
                                TEntityBaseEventInterface,
                                TEntityCreatedEventInterface,
                                TEntityRemovedEventInterface,
                                TEventEntityIdSetterGetter>
            where TEventEntityIdSetterGetter : IGetSetAggregateRootEntityEventEntityId<TEntityBaseEventClass, TEntityBaseEventInterface>, new()
        {
            public Entity(TAggregateRoot aggregateRoot) : base(aggregateRoot) { }
            public static new Collection CreateSelfManagingCollection(TAggregateRoot aggregate) => new Collection(aggregate);

            public new class Collection : Entity<TEntity,
                                              TEntityBaseEventClass,
                                              TEntityBaseEventInterface,
                                              TEntityCreatedEventInterface,
                                              TEventEntityIdSetterGetter>.Collection
            {
                public Collection(TAggregateRoot aggregate) : base(aggregate)
                {
                    aggregate.RegisterEventAppliers()
                             .For<TEntityRemovedEventInterface>(
                                 e =>
                                 {
                                     var id = IdGetterSetter.GetId(e);
                                     var entity = this[id];
                                     Entities.Remove(id);
                                     EntitiesInCreationOrder.Remove(entity);
                                 });
                }
            }
        }

        public abstract class Entity<TEntity,
                                     TEntityBaseEventClass,
                                     TEntityBaseEventInterface,
                                     TEntityCreatedEventInterface,
                                     TEventEntityIdSetterGetter> : Component<TEntity, TEntityBaseEventClass, TEntityBaseEventInterface>
            where TEntityBaseEventInterface : TAggregateRootBaseEventInterface
            where TEntityBaseEventClass : TAggregateRootBaseEventClass, TEntityBaseEventInterface
            where TEntityCreatedEventInterface : TEntityBaseEventInterface
            where TEntity : Entity<TEntity,
                                TEntityBaseEventClass,
                                TEntityBaseEventInterface,
                                TEntityCreatedEventInterface,
                                TEventEntityIdSetterGetter>
            where TEventEntityIdSetterGetter : IGetSetAggregateRootEntityEventEntityId<TEntityBaseEventClass, TEntityBaseEventInterface>, new()
        {
            protected static readonly TEventEntityIdSetterGetter IdGetterSetter = new TEventEntityIdSetterGetter();

            public Guid Id { get; private set; }

            protected Entity(TAggregateRoot aggregateRoot) : base(aggregateRoot: aggregateRoot, registerEventAppliers: false)
            {
                RegisterEventAppliers()
                    .For<TEntityCreatedEventInterface>(e => Id = IdGetterSetter.GetId((TEntityBaseEventClass)(object)e))
                    .IgnoreUnhandled<TEntityBaseEventInterface>();
            }

            public static Collection CreateSelfManagingCollection(TAggregateRoot aggregate) => new Collection(aggregate);

            protected override void RaiseEvent(TEntityBaseEventClass @event)
            {
                var id = IdGetterSetter.GetId(@event);
                if(id == Guid.Empty)
                {
                    IdGetterSetter.SetEntityId(@event, Id);
                }else if(id != Id)
                {
                    throw new Exception($"Attempted to raise event with EntityId: {id} frow within entity with EntityId: {Id}");
                }
                base.RaiseEvent(@event);
            }

            public class Collection : IReadOnlyAggregateRootEntityCollection<TEntity>
            {
                private readonly TAggregateRoot _aggregate;
                public Collection(TAggregateRoot aggregate)
                {
                    _aggregate = aggregate;
                    _aggregate.RegisterEventAppliers()
                              .For<TEntityCreatedEventInterface>(
                                  e =>
                                  {
                                      var entity = ObjectFactory<TEntity>.CreateInstance(_aggregate);

                                      Entities.Add(IdGetterSetter.GetId(e), entity);
                                      EntitiesInCreationOrder.Add(entity);
                                  })
                              .For<TEntityBaseEventInterface>(e => Entities[IdGetterSetter.GetId(e)].ApplyEvent(e));
                }

                public TEntity Add<TCreationEvent>(TCreationEvent creationEvent)
                    where TCreationEvent : TEntityBaseEventClass, TEntityCreatedEventInterface
                {
                    _aggregate.RaiseEvent(creationEvent);
                    var result = EntitiesInCreationOrder.Last();
                    result.EventHandlersEventDispatcher.Dispatch(creationEvent);
                    return result;
                }

                public IReadOnlyList<TEntity> InCreationOrder => EntitiesInCreationOrder;

                public bool TryGet(Guid id, out TEntity component) => Entities.TryGetValue(id, out component);
                [Pure]
                public bool Exists(Guid id) => Entities.ContainsKey(id);
                public TEntity Get(Guid id) => Entities[id];
                public TEntity this[Guid id] => Entities[id];

                protected readonly Dictionary<Guid, TEntity> Entities = new Dictionary<Guid, TEntity>();
                protected readonly List<TEntity> EntitiesInCreationOrder = new List<TEntity>();
            }
        }
    }
}
