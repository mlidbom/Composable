using System;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Events;

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
            internal abstract class RemovableNestedEntity<TEntity,
                                               TEntityId,
                                               TEntityEventImplementation,
                                               TEntityEvent,
                                               TEntityCreatedEvent,
                                               TEntityRemovedEvent,
                                               TEntityEventIdGetterSetter> :
                                                   NestedEntity<TEntity,
                                                       TEntityId,
                                                       TEntityEventImplementation,
                                                       TEntityEvent,
                                                       TEntityCreatedEvent,
                                                       TEntityEventIdGetterSetter>
                where TEntityEvent : class, TComponentEvent
                where TEntityEventImplementation : TComponentEventImplementation, TEntityEvent
                where TEntityCreatedEvent : TEntityEvent
                where TEntityRemovedEvent : TEntityEvent
                where TEntityEventIdGetterSetter :
                    IGetSetAggregateEntityEventEntityId<TEntityId, TEntityEventImplementation, TEntityEvent>
                where TEntity : NestedEntity<TEntity,
                                    TEntityId,
                                    TEntityEventImplementation,
                                    TEntityEvent,
                                    TEntityCreatedEvent,
                                    TEntityEventIdGetterSetter>
            {
                static RemovableNestedEntity() => AggregateTypeValidator<TEntity, TEntityEventImplementation, TEntityEvent>.AssertStaticStructureIsValid();

                protected RemovableNestedEntity(TComponent parent) : this(parent.TimeSource, parent.Publish, parent.RegisterEventAppliers())
                {
                }

                RemovableNestedEntity
                (IUtcTimeTimeSource timeSource,
                 Action<TEntityEventImplementation> raiseEventThroughParent,
                 IEventHandlerRegistrar<TEntityEvent> appliersRegistrar)
                    : base(timeSource, raiseEventThroughParent, appliersRegistrar)
                {
                    RegisterEventAppliers()
                        .IgnoreUnhandled<TEntityRemovedEvent>();
                }

                internal new static CollectionManager CreateSelfManagingCollection(TComponent parent)
                    =>
                        new CollectionManager(
                            parent: parent,
                            raiseEventThroughParent: parent.Publish,
                            appliersRegistrar: parent.RegisterEventAppliers());

                internal new class CollectionManager : EntityCollectionManager<TComponent,
                                                         TEntity,
                                                         TEntityId,
                                                         TEntityEventImplementation,
                                                         TEntityEvent,
                                                         TEntityCreatedEvent,
                                                         TEntityRemovedEvent,
                                                         TEntityEventIdGetterSetter>
                {
                    internal CollectionManager
                        (TComponent parent,
                         Action<TEntityEventImplementation> raiseEventThroughParent,
                         IEventHandlerRegistrar<TEntityEvent> appliersRegistrar)
                        : base(parent, raiseEventThroughParent, appliersRegistrar) {}
                }
            }
        }
    }
}
