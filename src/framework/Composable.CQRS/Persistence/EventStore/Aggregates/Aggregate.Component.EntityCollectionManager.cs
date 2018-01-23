using System;
using System.Linq;
using Composable.Messaging.Events;
using Composable.System.Reflection;

namespace Composable.Persistence.EventStore.Aggregates
{
    public abstract partial class Aggregate<TAggregate, TAggregateBaseEventClass, TAggregateBaseEventInterface>
        where TAggregate : Aggregate<TAggregate, TAggregateBaseEventClass, TAggregateBaseEventInterface>
        where TAggregateBaseEventInterface : class, IAggregateRootEvent
        where TAggregateBaseEventClass : AggregateRootEvent, TAggregateBaseEventInterface
    {
        public abstract partial class Component<TComponent, TComponentBaseEventClass, TComponentBaseEventInterface>
            where TComponentBaseEventInterface : class, TAggregateBaseEventInterface
            where TComponentBaseEventClass : TAggregateBaseEventClass, TComponentBaseEventInterface
            where TComponent : Component<TComponent, TComponentBaseEventClass, TComponentBaseEventInterface>
        {
            public class EntityCollectionManager<TParent,
                                                 TEntity,
                                                 TEntityId,
                                                 TEntityBaseEventClass,
                                                 TEntityBaseEventInterface,
                                                 TEntityCreatedEventInterface,
                                                 TEventEntityIdSetterGetter> : IEntityCollectionManager<TEntity,
                                                                                   TEntityId,
                                                                                   TEntityBaseEventClass,
                                                                                   TEntityCreatedEventInterface>
                where TEntityBaseEventInterface : class, TAggregateBaseEventInterface
                where TEntityCreatedEventInterface : TEntityBaseEventInterface
                where TEntityBaseEventClass : TEntityBaseEventInterface, TAggregateBaseEventClass
                where TEntity : Component<TEntity, TEntityBaseEventClass, TEntityBaseEventInterface>
                where TEventEntityIdSetterGetter : IGeTAggregateEntityEventEntityId<TEntityBaseEventInterface, TEntityId>, new()
            {
                protected static readonly TEventEntityIdSetterGetter IdGetter = new TEventEntityIdSetterGetter();

                protected readonly EntityCollection<TEntity, TEntityId> ManagedEntities;
                readonly Action<TEntityBaseEventClass> _raiseEventThroughParent;
                protected EntityCollectionManager
                    (TParent parent,
                     Action<TEntityBaseEventClass> raiseEventThroughParent,
                     IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar)
                {
                    ManagedEntities = new EntityCollection<TEntity, TEntityId>();
                    _raiseEventThroughParent = raiseEventThroughParent;
                    appliersRegistrar
                        .For<TEntityCreatedEventInterface>(
                            e =>
                            {
                                var entity = ObjectFactory<TEntity>.CreateInstance(parent);
                                ManagedEntities.Add(entity, IdGetter.GetId(e));
                            })
                        .For<TEntityBaseEventInterface>(e => ManagedEntities[IdGetter.GetId(e)].ApplyEvent(e));
                }

                public IReadOnlyEntityCollection<TEntity, TEntityId> Entities => ManagedEntities;

                public TEntity AddByPublishing<TCreationEvent>(TCreationEvent creationEvent)
                    where TCreationEvent : TEntityBaseEventClass, TEntityCreatedEventInterface
                {
                    _raiseEventThroughParent(creationEvent);
                    var result = ManagedEntities.InCreationOrder.Last();
                    result._eventHandlersEventDispatcher.Dispatch(creationEvent);
                    return result;
                }
            }
        }
    }
}
