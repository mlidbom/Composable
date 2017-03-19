using System;
using System.Linq;
using Composable.Messaging.Events;
using Composable.Persistence.EventSourcing;
using Composable.System.Reflection;

namespace Composable.Persistence.EventStore.AggregateRoots
{
    public abstract partial class AggregateRoot<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>
        where TAggregateRoot : AggregateRoot<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>
        where TAggregateRootBaseEventInterface : class, IAggregateRootEvent
        where TAggregateRootBaseEventClass : AggregateRootEvent, TAggregateRootBaseEventInterface
    {
        public abstract partial class Component<TComponent, TComponentBaseEventClass, TComponentBaseEventInterface>
            where TComponentBaseEventInterface : class, TAggregateRootBaseEventInterface
            where TComponentBaseEventClass : TAggregateRootBaseEventClass, TComponentBaseEventInterface
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
                where TEntityBaseEventInterface : class, TAggregateRootBaseEventInterface
                where TEntityCreatedEventInterface : TEntityBaseEventInterface
                where TEntityBaseEventClass : TEntityBaseEventInterface, TAggregateRootBaseEventClass
                where TEntity : Component<TEntity, TEntityBaseEventClass, TEntityBaseEventInterface>
                where TEventEntityIdSetterGetter : IGetAggregateRootEntityEventEntityId<TEntityBaseEventInterface, TEntityId>, new()
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

                public TEntity Add<TCreationEvent>(TCreationEvent creationEvent)
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
