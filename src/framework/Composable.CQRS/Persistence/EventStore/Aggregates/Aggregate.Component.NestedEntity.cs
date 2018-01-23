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
            public abstract class NestedEntity<TEntity,
                                               TEntityId,
                                               TEntityEventImplementation,
                                               TEntityEvent,
                                               TEntityCreatedEvent,
                                               TEntityEventIdGetterSetter> : NestedComponent<TEntity,
                                                                                 TEntityEventImplementation,
                                                                                 TEntityEvent>
                where TEntityEvent : class, TComponentEvent
                where TEntityEventImplementation : TComponentEventImplementation, TEntityEvent
                where TEntityCreatedEvent : TEntityEvent
                where TEntity : NestedEntity<TEntity,
                                    TEntityId,
                                    TEntityEventImplementation,
                                    TEntityEvent,
                                    TEntityCreatedEvent,
                                    TEntityEventIdGetterSetter>
                where TEntityEventIdGetterSetter : IGetSetAggregateEntityEventEntityId<TEntityId,
                                                       TEntityEventImplementation,
                                                       TEntityEvent>, new()
            {
                static NestedEntity() => AggregateTypeValidator<TEntity, TEntityEventImplementation, TEntityEvent>.AssertStaticStructureIsValid();

                static readonly TEntityEventIdGetterSetter IdGetterSetter = new TEntityEventIdGetterSetter();

                // ReSharper disable once UnusedMember.Global todo: coverage
                protected NestedEntity(TComponent parent)
                    : this(parent.TimeSource, parent.Publish, parent.RegisterEventAppliers()) { }

                protected NestedEntity
                    (IUtcTimeTimeSource timeSource,
                     Action<TEntityEventImplementation> raiseEventThroughParent,
                     IEventHandlerRegistrar<TEntityEvent> appliersRegistrar)
                    : base(timeSource, raiseEventThroughParent, appliersRegistrar, registerEventAppliers: false)
                {
                    RegisterEventAppliers()
                        .For<TEntityCreatedEvent>(e => Id = IdGetterSetter.GetId(e));
                }

                protected override void Publish(TEntityEventImplementation @event)
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
                    base.Publish(@event);
                }

                internal TEntityId Id { get; private set; }

                // ReSharper disable once UnusedMember.Global todo: coverage
                public  static CollectionManager CreateSelfManagingCollection(TComponent parent)//todo:tests
                  =>
                      new CollectionManager(
                          parent: parent,
                          raiseEventThroughParent: parent.Publish,
                          appliersRegistrar: parent.RegisterEventAppliers());

                public class CollectionManager : EntityCollectionManager<TComponent,
                                                         TEntity,
                                                         TEntityId,
                                                         TEntityEventImplementation,
                                                         TEntityEvent,
                                                         TEntityCreatedEvent,
                                                         TEntityEventIdGetterSetter>
                {
                    internal CollectionManager
                        (TComponent parent,
                         Action<TEntityEventImplementation> raiseEventThroughParent,
                         IEventHandlerRegistrar<TEntityEvent> appliersRegistrar)
                        : base(parent, raiseEventThroughParent, appliersRegistrar)
                    { }
                }
            }
        }
    }
}
