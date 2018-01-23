using System;
using Composable.Messaging.Events;

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
            internal class EntityCollectionManager<TParent,
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
                where TEntityBaseEventInterface : class, TAggregateBaseEventInterface
                where TEntityCreatedEventInterface : TEntityBaseEventInterface
                where TEntityRemovedEventInterface : TEntityBaseEventInterface
                where TEntityBaseEventClass : TEntityBaseEventInterface, TAggregateBaseEventClass
                where TEntity : Component<TEntity, TEntityBaseEventClass, TEntityBaseEventInterface>
                where TEventEntityIdSetterGetter :
                    IGetSeTAggregateEntityEventEntityId<TEntityId, TEntityBaseEventClass, TEntityBaseEventInterface>, new()
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
                            var id = IdGetter.GetId(e);
                            ManagedEntities.Remove(id);
                        });
                }
            }
        }
    }
}
