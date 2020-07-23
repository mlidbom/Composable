using System;
using System.Diagnostics.CodeAnalysis;
using Composable.Contracts;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Events;
using Composable.SystemCE.Reflection;

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
                                                       TEntityEvent>
            {
                static NestedEntity() => AggregateTypeValidator<TEntity, TEntityEventImplementation, TEntityEvent>.AssertStaticStructureIsValid();

                static readonly TEntityEventIdGetterSetter IdGetterSetter = Constructor.For<TEntityEventIdGetterSetter>.DefaultConstructor.Instance();

                // ReSharper disable once UnusedMember.Global todo: coverage
                protected NestedEntity(TComponent parent)
                    : this(parent.TimeSource, parent.Publish, parent.RegisterEventAppliers()) { }

#pragma warning disable 8618 //Review OK-ish: We guarantee that we never deliver out a null or default value from the public property. The private field cannot be marked nullable because it is a generic type argument and we don't want to constrain it to either classes or structs.
                protected NestedEntity
#pragma warning restore 8618
                (IUtcTimeTimeSource timeSource,
                 Action<TEntityEventImplementation> raiseEventThroughParent,
                 IEventHandlerRegistrar<TEntityEvent> appliersRegistrar)
                    : base(timeSource, raiseEventThroughParent, appliersRegistrar, registerEventAppliers: false)
                {
                    RegisterEventAppliers()
                        .For<TEntityCreatedEvent>(e => _id = IdGetterSetter.GetId(e));
                }

                protected override void Publish(TEntityEventImplementation @event)
                {
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
                        throw new Exception($"Attempted to raise event with EntityId: {id} from within entity with EntityId: {Id}");
                    }
                    base.Publish(@event);
                }

                TEntityId _id;
                internal TEntityId Id => Assert.Result.NotNullOrDefault(_id);

                // ReSharper disable once UnusedMember.Global todo: coverage
                public  static CollectionManager CreateSelfManagingCollection(TComponent parent)
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
