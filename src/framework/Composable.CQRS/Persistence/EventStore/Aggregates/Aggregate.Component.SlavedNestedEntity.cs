using System;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Events;
using Composable.SystemCE.ReflectionCE;

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
            ///<summary>
            /// An entity that is not created and removed through raising events.
            /// Instead it is automatically created and/or removed when another entity in the Aggregate object graph is added or removed.
            /// Inheritors must implement the add/remove behavior.
            /// Inheritors must ensure that the Id property is initialized correctly before any calls to RaiseEvent.
            /// Usually this is implemented within a nested class that inherits from <see cref="EntityCollectionManagerBase"/>
            /// </summary>
            public abstract class SlavedNestedEntity<TEntity,
                                               TEntityId,
                                               TEntityEventImplementation,
                                               TEntityEvent,
                                               TEntityEventIdGetterSetter> : NestedComponent<TEntity,
                                                                                 TEntityEventImplementation,
                                                                                 TEntityEvent>
                where TEntityId : notnull
                where TEntityEvent : class, TComponentEvent
                where TEntityEventImplementation : TComponentEventImplementation, TEntityEvent
                where TEntity : SlavedNestedEntity<TEntity,
                                    TEntityId,
                                    TEntityEventImplementation,
                                    TEntityEvent,
                                    TEntityEventIdGetterSetter>
                where TEntityEventIdGetterSetter : IGetSetAggregateEntityEventEntityId<TEntityId,
                                                       TEntityEventImplementation,
                                                       TEntityEvent>
            {
                static SlavedNestedEntity() => AggregateTypeValidator<TEntity, TEntityEventImplementation, TEntityEvent>.AssertStaticStructureIsValid();

                static readonly TEntityEventIdGetterSetter IdGetterSetter = Constructor.For<TEntityEventIdGetterSetter>.DefaultConstructor.Instance();

                protected SlavedNestedEntity(TComponent parent)
                    : this(parent.TimeSource, parent.Publish, parent.RegisterEventAppliers()) { }

#pragma warning disable CS8618 //Todo: Should this unused class even be retained? Non-nullable field is uninitialized. Consider declaring as nullable.
                protected SlavedNestedEntity
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
                    (IUtcTimeTimeSource timeSource,
                     Action<TEntityEventImplementation> raiseEventThroughParent,
                     IEventHandlerRegistrar<TEntityEvent> appliersRegistrar)
                    : base(timeSource, raiseEventThroughParent, appliersRegistrar, registerEventAppliers: false)
                {
                    RegisterEventAppliers()
                        .IgnoreUnhandled<TEntityEvent>();
                }

                protected override void Publish(TEntityEventImplementation @event)
                {
                    if(object.Equals(Id, default(TEntityId)!))
                    {
                        throw new Exception("You must assign Id before calling RaiseEvent");
                    }
                    var id = IdGetterSetter.GetId(@event);
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    if (Equals(id, default(TEntityId)!))
                        // ReSharper disable HeuristicUnreachableCode
                    {
                        IdGetterSetter.SetEntityId(@event, Id);
                    }
                    // ReSharper restore HeuristicUnreachableCode
                    else if (!Equals(id, Id))
                    {
                        throw new Exception($"Attempted to raise event with EntityId: {id} frow within entity with EntityId: {Id}");
                    }
                    base.Publish(@event);
                }

                // ReSharper disable once MemberCanBePrivate.Global
                // ReSharper disable once UnusedAutoPropertyAccessor.Global
                protected TEntityId Id { get; set; }


                public abstract class EntityCollectionManagerBase
                {
                    static readonly TEntityEventIdGetterSetter IdGetter = Constructor.For<TEntityEventIdGetterSetter>.DefaultConstructor.Instance();

                    readonly EntityCollection<TEntity, TEntityId> _managedEntities;
                    protected EntityCollectionManagerBase
                        (IEventHandlerRegistrar<TEntityEvent> appliersRegistrar)
                    {
                        _managedEntities = new EntityCollection<TEntity, TEntityId>();
                        appliersRegistrar
                            .For<TEntityEvent>(e => _managedEntities[IdGetter.GetId(e)].ApplyEvent(e));
                    }

                    public IReadOnlyEntityCollection<TEntity, TEntityId> Entities => _managedEntities;
                }
            }
        }
    }
}
