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
        public abstract class NestedEntity<TEntity, TEntityBaseEventClass, TEntityBaseEventInterface, TEntityCreatedEventInterface>
            where TEntityBaseEventInterface : TAggregateRootBaseEventInterface, IAggregateRootComponentEvent
            where TEntityBaseEventClass : TAggregateRootBaseEventClass, TEntityBaseEventInterface
            where TEntityCreatedEventInterface : TEntityBaseEventInterface, IAggregateRootEntityCreatedEvent
            where TEntity : NestedEntity<TEntity, TEntityBaseEventClass, TEntityBaseEventInterface, TEntityCreatedEventInterface>
        {
            private readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TEntityBaseEventInterface> _eventAppliersEventDispatcher =
                new CallMatchingHandlersInRegistrationOrderEventDispatcher<TEntityBaseEventInterface>();
            private readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TEntityBaseEventInterface> _eventHandlersEventDispatcher =
                new CallMatchingHandlersInRegistrationOrderEventDispatcher<TEntityBaseEventInterface>();

            protected IUtcTimeTimeSource TimeSource => AggregateRoot.TimeSource;
            protected TAggregateRoot AggregateRoot { get; private set; }
            public Guid Id { get; private set; }

            private void ApplyEvent(TEntityBaseEventInterface @event) { _eventAppliersEventDispatcher.Dispatch(@event); }

            protected NestedEntity()
            {
                _eventHandlersEventDispatcher.Register()
                                             .For<TEntityCreatedEventInterface>(e => Id = e.EntityId)
                                             .IgnoreUnhandled<TEntityBaseEventInterface>();
            }

            protected void RaiseEvent(TEntityBaseEventClass @event)
            {
                AggregateRoot.RaiseEvent(@event);
                _eventHandlersEventDispatcher.Dispatch(@event);
            }

            protected CallMatchingHandlersInRegistrationOrderEventDispatcher<TEntityBaseEventInterface>.RegistrationBuilder RegisterEventAppliers()
            {
                return _eventAppliersEventDispatcher.RegisterHandlers();
            }

            protected CallMatchingHandlersInRegistrationOrderEventDispatcher<TEntityBaseEventInterface>.RegistrationBuilder RegisterEventHandlers()
            {
                return _eventHandlersEventDispatcher.RegisterHandlers();
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
                    result._eventHandlersEventDispatcher.Dispatch(creationEvent);
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
