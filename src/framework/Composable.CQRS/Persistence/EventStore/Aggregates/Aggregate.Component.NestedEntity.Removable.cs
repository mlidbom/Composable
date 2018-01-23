using System;
using Composable.GenericAbstractions.Time;
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
            internal abstract class RemovableNestedEntity<TEntity,
                                               TEntityId,
                                               TEntityBaseEventClass,
                                               TEntityBaseEventInterface,
                                               TEntityCreatedEventInterface,
                                               TEntityRemovedEventInterface,
                                               TEventEntityIdSetterGetter> :
                                                   NestedEntity<TEntity,
                                                       TEntityId,
                                                       TEntityBaseEventClass,
                                                       TEntityBaseEventInterface,
                                                       TEntityCreatedEventInterface,
                                                       TEventEntityIdSetterGetter>
                where TEntityBaseEventInterface : class, TComponentBaseEventInterface
                where TEntityBaseEventClass : TComponentBaseEventClass, TEntityBaseEventInterface
                where TEntityCreatedEventInterface : TEntityBaseEventInterface
                where TEntityRemovedEventInterface : TEntityBaseEventInterface
                where TEventEntityIdSetterGetter :
                    IGetSeTAggregateEntityEventEntityId<TEntityId, TEntityBaseEventClass, TEntityBaseEventInterface>, new()
                where TEntity : NestedEntity<TEntity,
                                    TEntityId,
                                    TEntityBaseEventClass,
                                    TEntityBaseEventInterface,
                                    TEntityCreatedEventInterface,
                                    TEventEntityIdSetterGetter>
            {
                static RemovableNestedEntity() => AggregateTypeValidator<TEntity, TEntityBaseEventClass, TEntityBaseEventInterface>.AssertStaticStructureIsValid();

                protected RemovableNestedEntity(TComponent parent) : this(parent.TimeSource, parent.Publish, parent.RegisterEventAppliers())
                {
                }

                RemovableNestedEntity
                (IUtcTimeTimeSource timeSource,
                 Action<TEntityBaseEventClass> raiseEventThroughParent,
                 IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar)
                    : base(timeSource, raiseEventThroughParent, appliersRegistrar)
                {
                    RegisterEventAppliers()
                        .IgnoreUnhandled<TEntityRemovedEventInterface>();
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
                                                         TEntityBaseEventClass,
                                                         TEntityBaseEventInterface,
                                                         TEntityCreatedEventInterface,
                                                         TEntityRemovedEventInterface,
                                                         TEventEntityIdSetterGetter>
                {
                    internal CollectionManager
                        (TComponent parent,
                         Action<TEntityBaseEventClass> raiseEventThroughParent,
                         IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar)
                        : base(parent, raiseEventThroughParent, appliersRegistrar) {}
                }
            }
        }
    }
}
