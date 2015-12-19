using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Composable.CQRS.EventHandling;
using Composable.GenericAbstractions.Time;
using Composable.System.Reflection;

namespace Composable.CQRS.EventSourcing
{
    public interface IGetAggregateRootEntityEventEntityId<TEventInterface>
    {
        Guid GetId(TEventInterface @event);
    }

    public interface IGetSetAggregateRootEntityEventEntityId<TEventClass, TEventInterface> : IGetAggregateRootEntityEventEntityId<TEventInterface>
    {
        void SetEntityId(TEventClass @event, Guid id);
    }

    public abstract partial class AggregateRoot<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>
    {
        public abstract class Component<TComponent, TComponentBaseEventClass, TComponentBaseEventInterface>
            where TComponentBaseEventInterface : TAggregateRootBaseEventInterface
            where TComponentBaseEventClass : TAggregateRootBaseEventClass, TComponentBaseEventInterface
            where TComponent : Component<TComponent, TComponentBaseEventClass, TComponentBaseEventInterface>
        {
            private readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface> _eventAppliersEventDispatcher =
                new CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface>();
            internal readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface> EventHandlersEventDispatcher =
                new CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface>();

            protected IUtcTimeTimeSource TimeSource => AggregateRoot.TimeSource;
            private TAggregateRoot AggregateRoot { get; set; }

            internal void ApplyEvent(TComponentBaseEventInterface @event) { _eventAppliersEventDispatcher.Dispatch(@event); }

            protected Component(TAggregateRoot aggregateRoot) : this(aggregateRoot: aggregateRoot, registerEventAppliers: true) { }

            internal Component(TAggregateRoot aggregateRoot, bool registerEventAppliers)
            {
                AggregateRoot = aggregateRoot;
                EventHandlersEventDispatcher.Register()
                                            .IgnoreUnhandled<TComponentBaseEventInterface>();

                if(registerEventAppliers)
                {
                    AggregateRoot.RegisterEventAppliers()
                                 .For<TComponentBaseEventInterface>(ApplyEvent);
                }
            }

            protected void RaiseEvent(TComponentBaseEventClass @event)
            {
                AggregateRoot.RaiseEvent(@event);
                EventHandlersEventDispatcher.Dispatch(@event);
            }

            protected CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface>.RegistrationBuilder RegisterEventAppliers()
            {
                return _eventAppliersEventDispatcher.RegisterHandlers();
            }

            protected CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface>.RegistrationBuilder RegisterEventHandlers()
            {
                return EventHandlersEventDispatcher.RegisterHandlers();
            }

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

                    private readonly Dictionary<Guid, TEntity> _entities = new Dictionary<Guid, TEntity>();
                    private readonly List<TEntity> _entitiesInCreationOrder = new List<TEntity>();
                }
            }

            public abstract class NestedComponent<TNestedComponent, TNestedComponentBaseEventClass, TNestedComponentBaseEventInterface> :
                AggregateRoot<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>.
                    Component<TNestedComponent, TNestedComponentBaseEventClass, TNestedComponentBaseEventInterface>
                where TNestedComponentBaseEventInterface : TComponentBaseEventInterface
                where TNestedComponentBaseEventClass : TComponentBaseEventClass, TNestedComponentBaseEventInterface
                where TNestedComponent : NestedComponent<TNestedComponent, TNestedComponentBaseEventClass, TNestedComponentBaseEventInterface>
            {
                protected NestedComponent(TAggregateRoot aggregateRoot) : base(aggregateRoot) { }
            }
        }

        public abstract class Entity<TEntity, TEntityBaseEventClass, TEntityBaseEventInterface, TEntityCreatedEventInterface,
                                     TEventEntityIdSetterGetter> :
                                         Component<TEntity, TEntityBaseEventClass, TEntityBaseEventInterface>
            where TEntityBaseEventInterface : TAggregateRootBaseEventInterface
            where TEntityBaseEventClass : TAggregateRootBaseEventClass, TEntityBaseEventInterface
            where TEntityCreatedEventInterface : TEntityBaseEventInterface
            where TEntity :
                Entity<TEntity, TEntityBaseEventClass, TEntityBaseEventInterface, TEntityCreatedEventInterface, TEventEntityIdSetterGetter>
            where TEventEntityIdSetterGetter : IGetSetAggregateRootEntityEventEntityId<TEntityBaseEventClass, TEntityBaseEventInterface>, new()
        {
            private static readonly TEventEntityIdSetterGetter IdGetterSetter = new TEventEntityIdSetterGetter();

            public Guid Id { get; private set; }

            protected Entity(TAggregateRoot aggregateRoot) : base(aggregateRoot: aggregateRoot, registerEventAppliers: false)
            {
                RegisterEventAppliers()
                    .For<TEntityCreatedEventInterface>(e => Id = IdGetterSetter.GetId((TEntityBaseEventClass)(object)e))
                    .IgnoreUnhandled<TEntityBaseEventInterface>();
            }

            public static Collection CreateSelfManagingCollection(TAggregateRoot aggregate) => new Collection(aggregate);

            public class Collection : IReadOnlyAggregateRootEntityCollection<TEntity>
            {
                private readonly TAggregateRoot _aggregate;
                public Collection(TAggregateRoot aggregate)
                {
                    _aggregate = aggregate;
                    _aggregate.RegisterEventAppliers()
                              .For<TEntityCreatedEventInterface>(
                                  e =>
                                  {
                                      var entity = ObjectFactory<TEntity>.CreateInstance(_aggregate);

                                      _entities.Add(IdGetterSetter.GetId(e), entity);
                                      _entitiesInCreationOrder.Add(entity);
                                  })
                              .For<TEntityBaseEventInterface>(e => _entities[IdGetterSetter.GetId(e)].ApplyEvent(e));
                }

                public TEntity Add<TCreationEvent>(TCreationEvent creationEvent)
                    where TCreationEvent : TEntityBaseEventClass, TEntityCreatedEventInterface
                {
                    _aggregate.RaiseEvent(creationEvent);
                    var result = _entitiesInCreationOrder.Last();
                    result.EventHandlersEventDispatcher.Dispatch(creationEvent);
                    return result;
                }

                public IReadOnlyList<TEntity> InCreationOrder => _entitiesInCreationOrder;

                public bool TryGet(Guid id, out TEntity component) => _entities.TryGetValue(id, out component);
                [Pure]
                public bool Exists(Guid id) => _entities.ContainsKey(id);
                public TEntity Get(Guid id) => _entities[id];

                private readonly Dictionary<Guid, TEntity> _entities = new Dictionary<Guid, TEntity>();
                private readonly List<TEntity> _entitiesInCreationOrder = new List<TEntity>();
            }
        }
    }
}
