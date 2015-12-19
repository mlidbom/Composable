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
                                               TEntityBaseEventClass,
                                               TEntityBaseEventInterface,
                                               TEntityCreatedEventInterface,
                                               TEventEntityIdSetterGetter> : Component<TEntity, TEntityBaseEventClass, TEntityBaseEventInterface>
                where TEntityBaseEventInterface : TComponentBaseEventInterface
                where TEntityBaseEventClass : TComponentBaseEventClass, TEntityBaseEventInterface
                where TEntityCreatedEventInterface : TEntityBaseEventInterface
                where TEntity :
                    NestedEntity<TEntity, TEntityBaseEventClass, TEntityBaseEventInterface, TEntityCreatedEventInterface, TEventEntityIdSetterGetter>
                where TEventEntityIdSetterGetter : IGetSetAggregateRootEntityEventEntityId<TEntityBaseEventClass, TEntityBaseEventInterface>, new()
            {
                private static readonly TEventEntityIdSetterGetter IdGetterSetter = new TEventEntityIdSetterGetter();

                public Guid Id { get; private set; }

                protected NestedEntity(TComponent component) : base(aggregateRoot: component.AggregateRoot, registerEventAppliers: false)
                {
                    RegisterEventAppliers()
                        .For<TEntityCreatedEventInterface>(e => Id = IdGetterSetter.GetId(e))
                        .IgnoreUnhandled<TEntityBaseEventInterface>();
                }

                public static Collection CreateSelfManagingCollection(TComponent aggregate) => new Collection(aggregate);

                public class Collection : IReadOnlyAggregateRootEntityCollection<TEntity>
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
                                          _entities.Add(IdGetterSetter.GetId(e), entity);
                                          _entitiesInCreationOrder.Add(entity);
                                      })
                                  .For<TEntityBaseEventInterface>(e => _entities[IdGetterSetter.GetId(e)].ApplyEvent(e));
                    }

                    public TEntity Add<TCreationEvent>(TCreationEvent creationEvent)
                        where TCreationEvent : TEntityBaseEventClass, TEntityCreatedEventInterface
                    {
                        _component.RaiseEvent(creationEvent);
                        var result = _entitiesInCreationOrder.Last();
                        result.EventHandlersEventDispatcher.Dispatch(creationEvent);
                        return result;
                    }

                    public IReadOnlyList<TEntity> InCreationOrder => _entitiesInCreationOrder;

                    public bool TryGet(Guid id, out TEntity component) => _entities.TryGetValue(id, out component);
                    [Pure]
                    public bool Exists(Guid id) => _entities.ContainsKey(id);
                    public TEntity Get(Guid id) => _entities[id];
                    public TEntity this[Guid id] => _entities[id];

                    private readonly Dictionary<Guid, TEntity> _entities = new Dictionary<Guid, TEntity>();
                    private readonly List<TEntity> _entitiesInCreationOrder = new List<TEntity>();
                }
            }
        }
    }
}
