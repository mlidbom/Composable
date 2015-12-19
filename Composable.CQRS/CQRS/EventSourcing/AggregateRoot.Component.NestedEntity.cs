using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Composable.System.Reflection;

namespace Composable.CQRS.EventSourcing
{
    public abstract partial class AggregateRoot<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>
    {
        public abstract partial class Component<TComponent, TComponentBaseEventClass, TComponentBaseEventInterface>
            where TComponentBaseEventInterface : TAggregateRootBaseEventInterface
            where TComponentBaseEventClass : TAggregateRootBaseEventClass, TComponentBaseEventInterface
            where TComponent : Component<TComponent, TComponentBaseEventClass, TComponentBaseEventInterface>
        {            
            public abstract class NestedEntity<TEntity,
                                               TEntityId,
                                               TEntityBaseEventClass,
                                               TEntityBaseEventInterface,
                                               TEntityCreatedEventInterface,
                                               TEventEntityIdSetterGetter> : Component<TEntity, TEntityBaseEventClass, TEntityBaseEventInterface>
                where TEntityBaseEventInterface : TComponentBaseEventInterface
                where TEntityBaseEventClass : TComponentBaseEventClass, TEntityBaseEventInterface
                where TEntityCreatedEventInterface : TEntityBaseEventInterface
                where TEntity :
                    NestedEntity<TEntity, TEntityId, TEntityBaseEventClass, TEntityBaseEventInterface, TEntityCreatedEventInterface, TEventEntityIdSetterGetter>
                where TEventEntityIdSetterGetter : IGetSetAggregateRootEntityEventEntityId<TEntityId, TEntityBaseEventClass, TEntityBaseEventInterface>, new()
            {
                private readonly TComponent _component;
                protected static readonly TEventEntityIdSetterGetter IdGetterSetter = new TEventEntityIdSetterGetter();

                public TEntityId Id { get; private set; }

                protected NestedEntity(TComponent component) : base(aggregateRoot: component.AggregateRoot, registerEventAppliers: false)
                {
                    _component = component;
                    RegisterEventAppliers()
                        .For<TEntityCreatedEventInterface>(e => Id = IdGetterSetter.GetId(e))
                        .IgnoreUnhandled<TEntityBaseEventInterface>();
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
                    _component.RaiseEvent(@event);
                }

                public static Collection CreateSelfManagingCollection(TComponent aggregate) => new Collection(aggregate);

                public class Collection : IReadOnlyAggregateRootEntityCollection<TEntity, TEntityId>
                {
                    private readonly TComponent _component;
                    public Collection(TComponent component)
                    {
                        _component = component;
                        _component.RegisterEventAppliers()
                                  .For<TEntityCreatedEventInterface>(
                                      e =>
                                      {
                                          var entity = ObjectFactory<TEntity>.CreateInstance(_component);
                                          Entities.Add(IdGetterSetter.GetId(e), entity);
                                          EntitiesInCreationOrder.Add(entity);
                                      })
                                  .For<TEntityBaseEventInterface>(e => Entities[IdGetterSetter.GetId(e)].ApplyEvent(e));
                    }

                    public TEntity Add<TCreationEvent>(TCreationEvent creationEvent)
                        where TCreationEvent : TEntityBaseEventClass, TEntityCreatedEventInterface
                    {
                        _component.RaiseEvent(creationEvent);
                        var result = EntitiesInCreationOrder.Last();
                        result.EventHandlersEventDispatcher.Dispatch(creationEvent);
                        return result;
                    }

                    public IReadOnlyList<TEntity> InCreationOrder => EntitiesInCreationOrder;

                    public bool TryGet(TEntityId id, out TEntity component) => Entities.TryGetValue(id, out component);
                    [Pure]
                    public bool Exists(TEntityId id) => Entities.ContainsKey(id);
                    public TEntity Get(TEntityId id) => Entities[id];
                    public TEntity this[TEntityId id] => Entities[id];

                    protected readonly Dictionary<TEntityId, TEntity> Entities = new Dictionary<TEntityId, TEntity>();
                    protected readonly List<TEntity> EntitiesInCreationOrder = new List<TEntity>();
                }
            }
        }
    }
}
