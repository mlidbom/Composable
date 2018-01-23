using System;
using System.Collections;
using System.Collections.Generic;
using Composable.Messaging.Events;
using Composable.Persistence.EventStore.Aggregates;
using Composable.System.Linq;

namespace Composable.Persistence.EventStore.Query.Models
{
    //todo:complete including tests
    // ReSharper disable UnusedMember.Global
    abstract class SelfUpdatingSingleAggregateQueryModel<TQueryModel, TAggregateEvent>
        where TQueryModel : SelfUpdatingSingleAggregateQueryModel<TQueryModel, TAggregateEvent>
        where TAggregateEvent : class
    {
        readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateEvent> _eventAppliersEventDispatcher =
            new CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateEvent>();

        protected SelfUpdatingSingleAggregateQueryModel()
        {
            RegisterEventAppliers()
                .ForGenericEvent<IAggregateCreatedEvent>(e => {});
        }

        public void ApplyEvent(TAggregateEvent @event) { _eventAppliersEventDispatcher.Dispatch(@event); }
        public void ApplyEvents(IEnumerable<TAggregateEvent> @event)
        {
            @event.ForEach(_eventAppliersEventDispatcher.Dispatch);
        }

        IEventHandlerRegistrar<TAggregateEvent> RegisterEventAppliers() => _eventAppliersEventDispatcher.Register();

        public abstract class Entity<TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEventEntityIdGetter>
            where TEntityEvent : class, TAggregateEvent
            where TEntityCreatedEvent : TEntityEvent
            where TEntity : Entity<TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEventEntityIdGetter>
            where TEventEntityIdGetter : IGeTAggregateEntityEventEntityId<TEntityEvent, TEntityId>, new()
        {
            readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TEntityEvent> _eventAppliersEventDispatcher =
                new CallMatchingHandlersInRegistrationOrderEventDispatcher<TEntityEvent>();

            static readonly TEventEntityIdGetter IdGetter = new TEventEntityIdGetter();

            void ApplyEvent(TEntityEvent @event) { _eventAppliersEventDispatcher.Dispatch(@event); }

            protected IEventHandlerRegistrar<TEntityEvent> RegisterEventAppliers() => _eventAppliersEventDispatcher.Register();

            public static IReadOnlyEntityCollection<TEntity, TEntityId> CreateSelfManagingCollection(TQueryModel rootQueryModel) => new Collection(rootQueryModel);

            class Collection : IReadOnlyEntityCollection<TEntity, TEntityId>
            {
                public Collection(TQueryModel aggregate)
                {
                    aggregate.RegisterEventAppliers()
                         .For<TEntityCreatedEvent>(
                            e =>
                            {
                                var component = (TEntity)Activator.CreateInstance(typeof(TEntity), nonPublic:true);

                                _entities.Add(IdGetter.GetId(e), component);
                                _entitiesInCreationOrder.Add(component);
                            })
                        .For<TEntityEvent>(e => _entities[IdGetter.GetId(e)].ApplyEvent(e));
                }


                public IReadOnlyList<TEntity> InCreationOrder => _entitiesInCreationOrder;

                public bool TryGet(TEntityId id, out TEntity component) => _entities.TryGetValue(id, out component);
                public bool Exists(TEntityId id) => _entities.ContainsKey(id);
                public TEntity Get(TEntityId id) => _entities[id];
                public TEntity this[TEntityId id] => _entities[id];

                readonly Dictionary<TEntityId, TEntity> _entities = new Dictionary<TEntityId, TEntity>();
                readonly List<TEntity> _entitiesInCreationOrder = new List<TEntity>();

                public IEnumerator<TEntity> GetEnumerator() => _entitiesInCreationOrder.GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => _entitiesInCreationOrder.GetEnumerator();
            }
        }
    }
}
