using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Composable.CQRS.EventHandling;
using Composable.System.Reflection;

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
                                     TEntityRemovedEventInterface,
                                     TEventEntityIdSetterGetter> : Entity<TEntity,
                                                                       TEntityId,
                                                                       TEntityBaseEventClass,
                                                                       TEntityBaseEventInterface,
                                                                       TEntityCreatedEventInterface,
                                                                       TEventEntityIdSetterGetter>
            where TEntityBaseEventInterface : class, TAggregateRootBaseEventInterface
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
            public new static CollectionManager CreateSelfManagingCollection(TAggregateRoot parent) => new CollectionManager(parent: parent, raiseEventThroughParent: parent.RaiseEvent, appliersRegistrar: parent.RegisterEventAppliers());

            public class CollectionManager : EntityCollectionManager<TAggregateRoot, TEntity, TEntityId, TEntityBaseEventClass, TEntityBaseEventInterface, TEntityCreatedEventInterface, TEntityRemovedEventInterface, TEventEntityIdSetterGetter>
            {
                public CollectionManager(TAggregateRoot parent, Action<TEntityBaseEventClass> raiseEventThroughParent, IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar) : base(parent, raiseEventThroughParent, appliersRegistrar) { }
            }
        }
    }
}
