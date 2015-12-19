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
                                     TEntityRemovedEventInterface,
                                     TEventEntityIdSetterGetter> : Entity<TEntity,
                                                                       TEntityId,
                                                                       TEntityBaseEventClass,
                                                                       TEntityBaseEventInterface,
                                                                       TEntityCreatedEventInterface,
                                                                       TEventEntityIdSetterGetter>
            where TEntityBaseEventInterface : TAggregateRootBaseEventInterface
            where TEntityBaseEventClass : TAggregateRootBaseEventClass, TEntityBaseEventInterface
            where TEntityCreatedEventInterface : TEntityBaseEventInterface
            where TEntityRemovedEventInterface : TEntityBaseEventInterface
            where TEntity : Entity<TEntity,
                                TEntityId,
                                TEntityBaseEventClass,
                                TEntityBaseEventInterface,
                                TEntityCreatedEventInterface,
                                TEntityRemovedEventInterface,
                                TEventEntityIdSetterGetter>
            where TEventEntityIdSetterGetter : IGetSetAggregateRootEntityEventEntityId<TEntityId, TEntityBaseEventClass, TEntityBaseEventInterface>, new()
        {
            public Entity(TAggregateRoot aggregateRoot) : base(aggregateRoot) { }
            public static new Collection CreateSelfManagingCollection(TAggregateRoot aggregate) => new Collection(aggregate);

            public new class Collection : Entity<TEntity,
                                              TEntityId,
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
                                     _entities.Remove(IdGetterSetter.GetId(e));
                                 });
                }
            }
        }
    }
}
