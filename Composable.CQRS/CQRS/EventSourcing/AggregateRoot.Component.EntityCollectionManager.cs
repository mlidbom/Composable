using System;
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
                                                 TEntityRemovedEventInterface,
                                                 TEventEntityIdSetterGetter> : EntityCollectionManager<TParent,
                                                                                   TEntity,
                                                                                   TEntityId,
                                                                                   TEntityBaseEventClass,
                                                                                   TEntityBaseEventInterface,
                                                                                   TEntityCreatedEventInterface,
                                                                                   TEventEntityIdSetterGetter>
                where TEntityBaseEventInterface : class, TAggregateRootBaseEventInterface
                where TEntityCreatedEventInterface : TEntityBaseEventInterface
                where TEntityRemovedEventInterface : TEntityBaseEventInterface
                where TEntityBaseEventClass : TEntityBaseEventInterface, TAggregateRootBaseEventClass
                where TEntity : Component<TEntity, TEntityBaseEventClass, TEntityBaseEventInterface>
                where TEventEntityIdSetterGetter :
                    IGetSetAggregateRootEntityEventEntityId<TEntityId, TEntityBaseEventClass, TEntityBaseEventInterface>, new()
            {
                protected EntityCollectionManager
                    (TParent parent,
                     Action<TEntityBaseEventClass> raiseEventThroughParent,
                     IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar)
                    : base(parent, raiseEventThroughParent, appliersRegistrar)
                {
                    appliersRegistrar.For<TEntityRemovedEventInterface>(
                        e =>
                        {
                            var id = IdGetterSetter.GetId(e);
                            ManagedEntities.Remove(id);
                        });
                }
            }

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
                where TEventEntityIdSetterGetter :
                    IGetSetAggregateRootEntityEventEntityId<TEntityId, TEntityBaseEventClass, TEntityBaseEventInterface>, new()
            {
                protected static readonly TEventEntityIdSetterGetter IdGetterSetter = new TEventEntityIdSetterGetter();

                private readonly TParent _parent;
                protected readonly EntityCollection<TEntity, TEntityId> ManagedEntities;
                private readonly Action<TEntityBaseEventClass> _raiseEventThroughParent;
                protected EntityCollectionManager
                    (TParent parent,
                     Action<TEntityBaseEventClass> raiseEventThroughParent,
                     IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar)
                {
                    ManagedEntities = new EntityCollection<TEntity, TEntityId>();
                    _raiseEventThroughParent = raiseEventThroughParent;
                    _parent = parent;
                    appliersRegistrar
                        .For<TEntityCreatedEventInterface>(
                            e =>
                            {
                                var entity = ObjectFactory<TEntity>.CreateInstance(_parent);
                                ManagedEntities.Add(entity, IdGetterSetter.GetId(e));
                            })
                        .For<TEntityBaseEventInterface>(e => ManagedEntities[IdGetterSetter.GetId(e)].ApplyEvent(e));
                }

                public IReadOnlyEntityCollection<TEntity, TEntityId> Entities => ManagedEntities;

                public TEntity Add<TCreationEvent>(TCreationEvent creationEvent)
                    where TCreationEvent : TEntityBaseEventClass, TEntityCreatedEventInterface
                {
                    _raiseEventThroughParent(creationEvent);
                    var result = ManagedEntities.InCreationOrder.Last();
                    result.EventHandlersEventDispatcher.Dispatch(creationEvent);
                    return result;
                }
            }
        }
    }
}
