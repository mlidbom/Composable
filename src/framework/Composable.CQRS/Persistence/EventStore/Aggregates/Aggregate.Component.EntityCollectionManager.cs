using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Composable.Messaging.Events;
using Composable.System.Reflection;

namespace Composable.Persistence.EventStore.Aggregates
{
    public abstract partial class Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent>
        where TAggregate : Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent>
        where TAggregateEvent : class, IAggregateEvent
        where TAggregateEventImplementation : AggregateEvent, TAggregateEvent
    {
        public abstract partial class Component<TComponent, TComponentEventImplementation, TComponentEvent>
            where TComponentEvent : class, TAggregateEvent
            where TComponentEventImplementation : TAggregateEventImplementation, TComponentEvent
            where TComponent : Component<TComponent, TComponentEventImplementation, TComponentEvent>
        {
            public class EntityCollectionManager<TParent,
                                                 TEntity,
                                                 TEntityId,
                                                 TEntityEventImplementation,
                                                 TEntityEvent,
                                                 TEntityCreatedEvent,
                                                 TEntityEventIdGetterSetter> : IEntityCollectionManager<TEntity,
                                                                                   TEntityId,
                                                                                   TEntityEvent,
                                                                                   TEntityEventImplementation,
                                                                                   TEntityCreatedEvent>
                where TEntityEvent : class, TAggregateEvent
                where TEntityCreatedEvent : TEntityEvent
                where TEntityEventImplementation : TEntityEvent, TAggregateEventImplementation
                where TEntity : Component<TEntity, TEntityEventImplementation, TEntityEvent>
                where TEntityEventIdGetterSetter : IGetAggregateEntityEventEntityId<TEntityEvent, TEntityId>
            {
                protected static readonly TEntityEventIdGetterSetter IdGetter = Constructor.For<TEntityEventIdGetterSetter>.DefaultConstructor.Instance();

                protected readonly EntityCollection<TEntity, TEntityId> ManagedEntities;
                readonly Action<TEntityEventImplementation> _raiseEventThroughParent;
                protected EntityCollectionManager
                    (TParent parent,
                     Action<TEntityEventImplementation> raiseEventThroughParent,
                     IEventHandlerRegistrar<TEntityEvent> appliersRegistrar)
                {
                    ManagedEntities = new EntityCollection<TEntity, TEntityId>();
                    _raiseEventThroughParent = raiseEventThroughParent;
                    appliersRegistrar
                        .For<TEntityCreatedEvent>(
                            e =>
                            {
                                var entity = Constructor.For<TEntity>.WithArgument<TParent>.Instance(parent);
                                ManagedEntities.Add(entity, IdGetter.GetId(e));
                            })
                        .For<TEntityEvent>(e => ManagedEntities[IdGetter.GetId(e)].ApplyEvent(e));
                }

                public IReadOnlyEntityCollection<TEntity, TEntityId> Entities => ManagedEntities;

                public TEntity AddByPublishing<TCreationEvent>(TCreationEvent creationEvent)
                    where TCreationEvent : TEntityEventImplementation, TEntityCreatedEvent
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
