using System;

using Composable.GenericAbstractions.Time;

namespace Composable.CQRS.EventSourcing
{
  using Composable.Messaging.Events;

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
            public abstract class NestedEntity<TEntity,
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
                    IGetSetAggregateRootEntityEventEntityId<TEntityId, TEntityBaseEventClass, TEntityBaseEventInterface>, new()
                where TEntity : NestedEntity<TEntity,
                                    TEntityId,
                                    TEntityBaseEventClass,
                                    TEntityBaseEventInterface,
                                    TEntityCreatedEventInterface,
                                    TEventEntityIdSetterGetter>
            {
                protected NestedEntity(TComponent parent) : this(parent.TimeSource, parent.RaiseEvent, parent.RegisterEventAppliers())
                {
                }

                protected NestedEntity
                (IUtcTimeTimeSource timeSource,
                 Action<TEntityBaseEventClass> raiseEventThroughParent,
                 IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar)
                    : base(timeSource, raiseEventThroughParent, appliersRegistrar)
                {
                    RegisterEventAppliers()
                        .IgnoreUnhandled<TEntityRemovedEventInterface>();
                }

                public new static CollectionManager CreateSelfManagingCollection(TComponent parent)
                    =>
                        new CollectionManager(
                            parent: parent,
                            raiseEventThroughParent: parent.RaiseEvent,
                            appliersRegistrar: parent.RegisterEventAppliers());

                public new class CollectionManager : EntityCollectionManager<TComponent,
                                                         TEntity,
                                                         TEntityId,
                                                         TEntityBaseEventClass,
                                                         TEntityBaseEventInterface,
                                                         TEntityCreatedEventInterface,
                                                         TEntityRemovedEventInterface,
                                                         TEventEntityIdSetterGetter>
                {
                    public CollectionManager
                        (TComponent parent,
                         Action<TEntityBaseEventClass> raiseEventThroughParent,
                         IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar)
                        : base(parent, raiseEventThroughParent, appliersRegistrar) {}
                }
            }
        }
    }
}
