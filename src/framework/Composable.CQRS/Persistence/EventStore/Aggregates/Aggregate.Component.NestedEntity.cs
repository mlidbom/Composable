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
            public abstract class NestedEntity<TEntity,
                                               TEntityId,
                                               TEntityBaseEventClass,
                                               TEntityBaseEventInterface,
                                               TEntityCreatedEventInterface,
                                               TEventEntityIdSetterGetter> : NestedComponent<TEntity,
                                                                                 TEntityBaseEventClass,
                                                                                 TEntityBaseEventInterface>
                where TEntityBaseEventInterface : class, TComponentBaseEventInterface
                where TEntityBaseEventClass : TComponentBaseEventClass, TEntityBaseEventInterface
                where TEntityCreatedEventInterface : TEntityBaseEventInterface
                where TEntity : NestedEntity<TEntity,
                                    TEntityId,
                                    TEntityBaseEventClass,
                                    TEntityBaseEventInterface,
                                    TEntityCreatedEventInterface,
                                    TEventEntityIdSetterGetter>
                where TEventEntityIdSetterGetter : IGetSeTAggregateEntityEventEntityId<TEntityId,
                                                       TEntityBaseEventClass,
                                                       TEntityBaseEventInterface>, new()
            {
                static NestedEntity() => AggregateTypeValidator<TEntity, TEntityBaseEventClass, TEntityBaseEventInterface>.AssertStaticStructureIsValid();

                static readonly TEventEntityIdSetterGetter IdGetterSetter = new TEventEntityIdSetterGetter();

                // ReSharper disable once UnusedMember.Global todo: coverage
                protected NestedEntity(TComponent parent)
                    : this(parent.TimeSource, parent.Publish, parent.RegisterEventAppliers()) { }

                protected NestedEntity
                    (IUtcTimeTimeSource timeSource,
                     Action<TEntityBaseEventClass> raiseEventThroughParent,
                     IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar)
                    : base(timeSource, raiseEventThroughParent, appliersRegistrar, registerEventAppliers: false)
                {
                    RegisterEventAppliers()
                        .For<TEntityCreatedEventInterface>(e => Id = IdGetterSetter.GetId(e));
                }

                protected override void Publish(TEntityBaseEventClass @event)
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
                                                         TEntityBaseEventClass,
                                                         TEntityBaseEventInterface,
                                                         TEntityCreatedEventInterface,
                                                         TEventEntityIdSetterGetter>
                {
                    internal CollectionManager
                        (TComponent parent,
                         Action<TEntityBaseEventClass> raiseEventThroughParent,
                         IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar)
                        : base(parent, raiseEventThroughParent, appliersRegistrar)
                    { }
                }
            }
        }
    }
}
