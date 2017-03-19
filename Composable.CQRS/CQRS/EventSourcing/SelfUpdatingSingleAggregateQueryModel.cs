using System;
using System.Collections;
using System.Collections.Generic;
using Composable.Messaging.Events;
using Composable.System.Linq;

namespace Composable.CQRS.EventSourcing
{
    //todo:complete including tests
    // ReSharper disable UnusedMember.Global
    abstract class SelfUpdatingSingleAggregateQueryModel<TRootQueryModel, TAggregateRootBaseEventInterface>
        where TRootQueryModel : SelfUpdatingSingleAggregateQueryModel<TRootQueryModel, TAggregateRootBaseEventInterface>
        where TAggregateRootBaseEventInterface : class
    {
        readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateRootBaseEventInterface> _eventAppliersEventDispatcher =
            new CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateRootBaseEventInterface>();

        protected SelfUpdatingSingleAggregateQueryModel()
        {
            RegisterEventAppliers()
                .ForGenericEvent<IAggregateRootCreatedEvent>(e => {});
        }

        public void ApplyEvent(TAggregateRootBaseEventInterface @event) { _eventAppliersEventDispatcher.Dispatch(@event); }
        public void ApplyEvents(IEnumerable<TAggregateRootBaseEventInterface> @event)
        {
            @event.ForEach(_eventAppliersEventDispatcher.Dispatch);
        }

        IEventHandlerRegistrar<TAggregateRootBaseEventInterface> RegisterEventAppliers() => _eventAppliersEventDispatcher.Register();

        public abstract class Entity<TEntitity, TEntityId, TEntityBaseEventClass, TEntityBaseEventInterface, TEntityCreatedEventInterface, TEventEntityIdGetter>
            where TEntityBaseEventInterface : class, TAggregateRootBaseEventInterface
            where TEntityCreatedEventInterface : TEntityBaseEventInterface
            where TEntitity : Entity<TEntitity, TEntityId, TEntityBaseEventClass, TEntityBaseEventInterface, TEntityCreatedEventInterface, TEventEntityIdGetter>
            where TEventEntityIdGetter : IGetAggregateRootEntityEventEntityId<TEntityBaseEventInterface, TEntityId>, new()
        {
            readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TEntityBaseEventInterface> _eventAppliersEventDispatcher =
                new CallMatchingHandlersInRegistrationOrderEventDispatcher<TEntityBaseEventInterface>();

            static readonly TEventEntityIdGetter IdGetter = new TEventEntityIdGetter();

            void ApplyEvent(TEntityBaseEventInterface @event) { _eventAppliersEventDispatcher.Dispatch(@event); }

            protected IEventHandlerRegistrar<TEntityBaseEventInterface> RegisterEventAppliers() => _eventAppliersEventDispatcher.Register();

            public static IReadOnlyEntityCollection<TEntitity, TEntityId> CreateSelfManagingCollection(TRootQueryModel rootQueryModel) => new Collection(rootQueryModel);

            class Collection : IReadOnlyEntityCollection<TEntitity, TEntityId>
            {
                public Collection(TRootQueryModel aggregate)
                {
                    aggregate.RegisterEventAppliers()
                         .For<TEntityCreatedEventInterface>(
                            e =>
                            {
                                var component = (TEntitity)Activator.CreateInstance(typeof(TEntitity), nonPublic:true);

                                _entities.Add(IdGetter.GetId(e), component);
                                _entitiesInCreationOrder.Add(component);
                            })
                        .For<TEntityBaseEventInterface>(e => _entities[IdGetter.GetId(e)].ApplyEvent(e));
                }


                public IReadOnlyList<TEntitity> InCreationOrder => _entitiesInCreationOrder;

                public bool TryGet(TEntityId id, out TEntitity component) => _entities.TryGetValue(id, out component);
                public bool Exists(TEntityId id) => _entities.ContainsKey(id);
                public TEntitity Get(TEntityId id) => _entities[id];
                public TEntitity this[TEntityId id] => _entities[id];

                readonly Dictionary<TEntityId, TEntitity> _entities = new Dictionary<TEntityId, TEntitity>();
                readonly List<TEntitity> _entitiesInCreationOrder = new List<TEntitity>();

                public IEnumerator<TEntitity> GetEnumerator() => _entitiesInCreationOrder.GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => _entitiesInCreationOrder.GetEnumerator();
            }
        }
    }
}
