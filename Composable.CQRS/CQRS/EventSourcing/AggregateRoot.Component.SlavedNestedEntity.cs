using System;
using Composable.CQRS.EventSourcing;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Events;

// ReSharper disable UnusedMember.Global todo:tests

namespace Composable.CQRS.CQRS.EventSourcing
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
            ///<summary>
            /// An entity that is not created and removed through raising events.
            /// Instead it is automatically created and/or removed when another entity in the Aggregate object graph is added or removed.
            /// Inheritors must implement the add/remove behavior.
            /// Inheritors must ensure that the Id property is initialized correctly before any calls to RaiseEvent.
            /// Usually this is implemented within a nested class that inherits from <see cref="EntityCollectionManagerBase"/>
            /// </summary>
            public abstract class SlavedNestedEntity<TEntity,
                                               TEntityId,
                                               TEntityBaseEventClass,
                                               TEntityBaseEventInterface,
                                               TEventEntityIdSetterGetter> : NestedComponent<TEntity,
                                                                                 TEntityBaseEventClass,
                                                                                 TEntityBaseEventInterface>
                where TEntityBaseEventInterface : class, TComponentBaseEventInterface
                where TEntityBaseEventClass : TComponentBaseEventClass, TEntityBaseEventInterface
                where TEntity : SlavedNestedEntity<TEntity,
                                    TEntityId,
                                    TEntityBaseEventClass,
                                    TEntityBaseEventInterface,
                                    TEventEntityIdSetterGetter>
                where TEventEntityIdSetterGetter : IGetSetAggregateRootEntityEventEntityId<TEntityId,
                                                       TEntityBaseEventClass,
                                                       TEntityBaseEventInterface>, new()
            {
                static readonly TEventEntityIdSetterGetter IdGetterSetter = new TEventEntityIdSetterGetter();

                protected SlavedNestedEntity(TComponent parent)
                    : this(parent.TimeSource, parent.RaiseEvent, parent.RegisterEventAppliers()) { }

                protected SlavedNestedEntity
                    (IUtcTimeTimeSource timeSource,
                     Action<TEntityBaseEventClass> raiseEventThroughParent,
                     IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar)
                    : base(timeSource, raiseEventThroughParent, appliersRegistrar, registerEventAppliers: false)
                {
                    RegisterEventAppliers()
                        .IgnoreUnhandled<TEntityBaseEventInterface>();
                }

                protected override void RaiseEvent(TEntityBaseEventClass @event)
                {
                    if(object.Equals(Id, default(TEntityId)))
                    {
                        throw new Exception("You must assign Id before calling RaiseEvent");
                    }
                    var id = IdGetterSetter.GetId(@event);
                    if (Equals(id, default(TEntityId)))
                    {
                        IdGetterSetter.SetEntityId(@event, Id);
                    }
                    else if (!Equals(id, Id))
                    {
                        throw new Exception($"Attempted to raise event with EntityId: {id} frow within entity with EntityId: {Id}");
                    }
                    base.RaiseEvent(@event);
                }

                // ReSharper disable once MemberCanBePrivate.Global
                // ReSharper disable once UnusedAutoPropertyAccessor.Global
                protected TEntityId Id { get; set; }


                public abstract class EntityCollectionManagerBase
                {
                    static readonly TEventEntityIdSetterGetter IdGetter = new TEventEntityIdSetterGetter();

                    readonly EntityCollection<TEntity, TEntityId> _managedEntities;
                    protected EntityCollectionManagerBase
                        (IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar)
                    {
                        _managedEntities = new EntityCollection<TEntity, TEntityId>();
                        appliersRegistrar
                            .For<TEntityBaseEventInterface>(e => _managedEntities[IdGetter.GetId(e)].ApplyEvent(e));
                    }

                    public IReadOnlyEntityCollection<TEntity, TEntityId> Entities => _managedEntities;
                }
            }
        }
    }
}
