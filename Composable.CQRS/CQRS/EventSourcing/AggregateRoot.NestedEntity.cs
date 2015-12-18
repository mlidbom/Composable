using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Composable.CQRS.EventHandling;
using Composable.GenericAbstractions.Time;

namespace Composable.CQRS.EventSourcing
{   
    public abstract partial class AggregateRoot<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>
    {
        public abstract class Component<TComponent, TComponentBaseEventClass, TComponentBaseEventInterface>
           where TComponentBaseEventInterface : TAggregateRootBaseEventInterface, IAggregateRootComponentEvent
           where TComponentBaseEventClass : TAggregateRootBaseEventClass, TComponentBaseEventInterface
           where TComponent : Component<TComponent, TComponentBaseEventClass, TComponentBaseEventInterface>
        {
            private readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface> _eventAppliersEventDispatcher =
                new CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface>();
            internal readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface> EventHandlersEventDispatcher =
                new CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface>();

            protected IUtcTimeTimeSource TimeSource => AggregateRoot.TimeSource;
            protected TAggregateRoot AggregateRoot { get; set; }

            internal void ApplyEvent(TComponentBaseEventInterface @event) { _eventAppliersEventDispatcher.Dispatch(@event); }

            protected Component()
            {
                EventHandlersEventDispatcher.Register()
                                             .IgnoreUnhandled<TComponentBaseEventInterface>();
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
        }

        public abstract class NestedEntity<TEntity, TEntityBaseEventClass, TEntityBaseEventInterface, TEntityCreatedEventInterface> : Component<TEntity, TEntityBaseEventClass, TEntityBaseEventInterface>
            where TEntityBaseEventInterface : TAggregateRootBaseEventInterface, IAggregateRootComponentEvent
            where TEntityBaseEventClass : TAggregateRootBaseEventClass, TEntityBaseEventInterface
            where TEntityCreatedEventInterface : TEntityBaseEventInterface, IAggregateRootEntityCreatedEvent
            where TEntity : NestedEntity<TEntity, TEntityBaseEventClass, TEntityBaseEventInterface, TEntityCreatedEventInterface>
        {
            public Guid Id { get; private set; }

            protected NestedEntity()
            {
                RegisterEventAppliers()
                    .For<TEntityCreatedEventInterface>(e => Id = e.EntityId)
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
                                var entity = (TEntity)Activator.CreateInstance(typeof(TEntity), nonPublic: true);
                                entity.AggregateRoot = _aggregate;

                                _entities.Add(e.EntityId, entity);
                                _entitiesInCreationOrder.Add(entity);
                            })
                        .For<TEntityBaseEventInterface>(e => _entities[e.EntityId].ApplyEvent(e));
                }

   

                public TEntity Add<TCreationEvent>(TCreationEvent creationEvent) where TCreationEvent : TEntityBaseEventClass, TEntityCreatedEventInterface
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
