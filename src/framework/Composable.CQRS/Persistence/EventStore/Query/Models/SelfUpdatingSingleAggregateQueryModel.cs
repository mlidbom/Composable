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
    abstract class SelfUpdatingSingleAggregateQueryModel<TRootQueryModel, TAggregateBaseEventInterface>
        where TRootQueryModel : SelfUpdatingSingleAggregateQueryModel<TRootQueryModel, TAggregateBaseEventInterface>
        where TAggregateBaseEventInterface : class
    {
        readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateBaseEventInterface> _eventAppliersEventDispatcher =
            new CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateBaseEventInterface>();

        protected SelfUpdatingSingleAggregateQueryModel()
        {
            RegisterEventAppliers()
                .ForGenericEvent<IAggregateRootCreatedEvent>(e => {});
        }

        public void ApplyEvent(TAggregateBaseEventInterface @event) { _eventAppliersEventDispatcher.Dispatch(@event); }
        public void ApplyEvents(IEnumerable<TAggregateBaseEventInterface> @event)
        {
            @event.ForEach(_eventAppliersEventDispatcher.Dispatch);
        }

        IEventHandlerRegistrar<TAggregateBaseEventInterface> RegisterEventAppliers() => _eventAppliersEventDispatcher.Register();

        public abstract class Entity<TEntity, TEntityId, TEntityBaseEventClass, TEntityBaseEventInterface, TEntityCreatedEventInterface, TEventEntityIdGetter>
            where TEntityBaseEventInterface : class, TAggregateBaseEventInterface
            where TEntityCreatedEventInterface : TEntityBaseEventInterface
            where TEntity : Entity<TEntity, TEntityId, TEntityBaseEventClass, TEntityBaseEventInterface, TEntityCreatedEventInterface, TEventEntityIdGetter>
            where TEventEntityIdGetter : IGeTAggregateEntityEventEntityId<TEntityBaseEventInterface, TEntityId>, new()
        {
            readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TEntityBaseEventInterface> _eventAppliersEventDispatcher =
                new CallMatchingHandlersInRegistrationOrderEventDispatcher<TEntityBaseEventInterface>();

            static readonly TEventEntityIdGetter IdGetter = new TEventEntityIdGetter();

            void ApplyEvent(TEntityBaseEventInterface @event) { _eventAppliersEventDispatcher.Dispatch(@event); }

            protected IEventHandlerRegistrar<TEntityBaseEventInterface> RegisterEventAppliers() => _eventAppliersEventDispatcher.Register();

            public static IReadOnlyEntityCollection<TEntity, TEntityId> CreateSelfManagingCollection(TRootQueryModel rootQueryModel) => new Collection(rootQueryModel);

            class Collection : IReadOnlyEntityCollection<TEntity, TEntityId>
            {
                public Collection(TRootQueryModel aggregate)
                {
                    aggregate.RegisterEventAppliers()
                         .For<TEntityCreatedEventInterface>(
                            e =>
                            {
                                var component = (TEntity)Activator.CreateInstance(typeof(TEntity), nonPublic:true);

                                _entities.Add(IdGetter.GetId(e), component);
                                _entitiesInCreationOrder.Add(component);
                            })
                        .For<TEntityBaseEventInterface>(e => _entities[IdGetter.GetId(e)].ApplyEvent(e));
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
