using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Composable.CQRS.EventHandling;
using Composable.GenericAbstractions.Time;
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
            public abstract class NestedEntity<TEntity,
                                               TEntityId,
                                               TEntityBaseEventClass,
                                               TEntityBaseEventInterface,
                                               TEntityCreatedEventInterface,
                                               TEventEntityIdSetterGetter> : Entity<TEntity, TEntityId, TEntityBaseEventClass, TEntityBaseEventInterface, TEntityCreatedEventInterface, TEventEntityIdSetterGetter>
                where TEntityBaseEventInterface : class, TComponentBaseEventInterface
                where TEntityBaseEventClass : TComponentBaseEventClass, TEntityBaseEventInterface
                where TEntityCreatedEventInterface : TEntityBaseEventInterface
                where TEntity :
                    NestedEntity<TEntity, TEntityId, TEntityBaseEventClass, TEntityBaseEventInterface, TEntityCreatedEventInterface, TEventEntityIdSetterGetter>
                where TEventEntityIdSetterGetter : IGetSetAggregateRootEntityEventEntityId<TEntityId, TEntityBaseEventClass, TEntityBaseEventInterface>, new()
            {
                private readonly TComponent _parent;

                public TEntityId Id { get; private set; }

                protected NestedEntity(TComponent parent) : this(timeSource: parent.TimeSource, raiseEventThroughParent:parent.RaiseEvent, appliersRegistrar: parent.RegisterEventAppliers(), registerEventAppliers: false)
                {
                    _parent = parent;
                    RegisterEventAppliers()
                        .For<TEntityCreatedEventInterface>(e => Id = IdGetterSetter.GetId(e))
                        .IgnoreUnhandled<TEntityBaseEventInterface>();
                }

                public NestedEntity(IUtcTimeTimeSource timeSource, Action<TEntityBaseEventClass> raiseEventThroughParent, IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar, bool registerEventAppliers) 
                    : base(timeSource: timeSource, raiseEventThroughParent: raiseEventThroughParent, appliersRegistrar: appliersRegistrar, registerEventAppliers: registerEventAppliers)
                {
                }

                protected override void RaiseEvent(TEntityBaseEventClass @event)
                {
                    var id = IdGetterSetter.GetId(@event);
                    if (Equals(id, default(TEntityId)))
                    {
                        IdGetterSetter.SetEntityId(@event, Id);
                    }
                    else if (!Equals(id, Id))
                    {
                        throw new Exception($"Attempted to raise event with EntityId: {id} frow within entity with EntityId: {Id}");
                    }
                    _parent.RaiseEvent(@event);
                }

                public static CollectionManager CreateSelfManagingCollection(TComponent parent) => new CollectionManager(parent: parent, raiseEventThroughParent: parent.RaiseEvent, appliersRegistrar: parent.RegisterEventAppliers());

                public class CollectionManager : EntityCollectionManager<TComponent, TEntity, TEntityId, TEntityBaseEventClass, TEntityBaseEventInterface, TEntityCreatedEventInterface, TEventEntityIdSetterGetter>
                {
                    public CollectionManager(TComponent parent, Action<TEntityBaseEventClass> raiseEventThroughParent, IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar) : base(parent, raiseEventThroughParent, appliersRegistrar) {}
                }
            }
        }
    }
}
