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
                                     TEntityId,
                                     TEntityBaseEventClass,
                                     TEntityBaseEventInterface,
                                     TEntityCreatedEventInterface,
                                     TEventEntityIdSetterGetter> : Component<TEntity, TEntityBaseEventClass, TEntityBaseEventInterface>
            where TEntityBaseEventInterface : TAggregateRootBaseEventInterface
            where TEntityBaseEventClass : TAggregateRootBaseEventClass, TEntityBaseEventInterface
            where TEntityCreatedEventInterface : TEntityBaseEventInterface
            where TEntity : Entity<TEntity,
                                TEntityId,
                                TEntityBaseEventClass,
                                TEntityBaseEventInterface,
                                TEntityCreatedEventInterface,
                                TEventEntityIdSetterGetter>
            where TEventEntityIdSetterGetter : IGetSetAggregateRootEntityEventEntityId<TEntityId, TEntityBaseEventClass, TEntityBaseEventInterface>, new()
        {
            protected static readonly TEventEntityIdSetterGetter IdGetterSetter = new TEventEntityIdSetterGetter();

            public TEntityId Id { get; private set; }

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
                if (Equals(id, default(TEntityId)))
                {
                    IdGetterSetter.SetEntityId(@event, Id);
                }
                else if (!Equals(id, Id))
                {
                    throw new Exception($"Attempted to raise event with EntityId: {id} frow within entity with EntityId: {Id}");
                }
                base.RaiseEvent(@event);
            }

            public class Collection : IEntityCollectionManager<TEntity, TEntityId, TEntityBaseEventClass, TEntityCreatedEventInterface>
            {
                private readonly TAggregateRoot _aggregate;
                protected readonly EntityCollection<TEntity, TEntityId> _entities;
                public Collection(TAggregateRoot aggregate)
                {
                    _entities = new EntityCollection<TEntity, TEntityId>();
                    _aggregate = aggregate;
                    _aggregate.RegisterEventAppliers()
                              .For<TEntityCreatedEventInterface>(
                                  e =>
                                  {
                                      var entity = ObjectFactory<TEntity>.CreateInstance(_aggregate);
                                      _entities.Add(entity, IdGetterSetter.GetId(e));
                                  })
                              .For<TEntityBaseEventInterface>(e => _entities[IdGetterSetter.GetId(e)].ApplyEvent(e));
                }

                public IReadOnlyEntityCollection<TEntity, TEntityId> Entities => _entities;

                public TEntity Add<TCreationEvent>(TCreationEvent creationEvent)
                    where TCreationEvent : TEntityBaseEventClass, TEntityCreatedEventInterface
                {
                    _aggregate.RaiseEvent(creationEvent);
                    var result = _entities.InCreationOrder.Last();
                    result.EventHandlersEventDispatcher.Dispatch(creationEvent);
                    return result;
                }
                
            }
        }
    }
}
