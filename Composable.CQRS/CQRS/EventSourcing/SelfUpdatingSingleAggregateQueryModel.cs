using System;
using System.Collections.Generic;
using Composable.CQRS.EventHandling;
using Composable.System.Linq;

namespace Composable.CQRS.EventSourcing
{
    public abstract class SelfUpdatingSingleAggregateQueryModel<TRootQueryModel, TAggregateRootBaseEventInterface>
        where TRootQueryModel : SelfUpdatingSingleAggregateQueryModel<TRootQueryModel, TAggregateRootBaseEventInterface>
    {
        readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateRootBaseEventInterface> _eventAppliersEventDispatcher =
            new CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateRootBaseEventInterface>();


        public Guid Id { get; private set; }

        protected SelfUpdatingSingleAggregateQueryModel()
        {
            RegisterEventAppliers()
                .ForGenericEvent<IAggregateRootCreatedEvent>(e => Id = e.AggregateRootId);
        }

        public void ApplyEvent(TAggregateRootBaseEventInterface @event) { _eventAppliersEventDispatcher.Dispatch(@event); }
        public void ApplyEvents(IEnumerable<TAggregateRootBaseEventInterface> @event)
        {
            @event.ForEach(_eventAppliersEventDispatcher.Dispatch);
        }

        protected CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateRootBaseEventInterface>.RegistrationBuilder RegisterEventAppliers()
        {
            return _eventAppliersEventDispatcher.RegisterHandlers();
        }


        public abstract class Entity<TEntitity, TEntityId, TEntityBaseEventClass, TEntityBaseEventInterface, TEntityCreatedEventInterface, TEventEntityIdGetter>
            where TEntityBaseEventInterface : TAggregateRootBaseEventInterface
            where TEntityCreatedEventInterface : TEntityBaseEventInterface
            where TEntitity : Entity<TEntitity, TEntityId, TEntityBaseEventClass, TEntityBaseEventInterface, TEntityCreatedEventInterface, TEventEntityIdGetter>
            where TEventEntityIdGetter : IGetAggregateRootEntityEventEntityId<TEntityBaseEventInterface, TEntityId>, new()
        {
            private readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TEntityBaseEventInterface> _eventAppliersEventDispatcher =
                new CallMatchingHandlersInRegistrationOrderEventDispatcher<TEntityBaseEventInterface>();

            private static readonly TEventEntityIdGetter IdGetter = new TEventEntityIdGetter();

            protected TRootQueryModel RootQueryModel { get; private set; }

            private void ApplyEvent(TEntityBaseEventInterface @event) { _eventAppliersEventDispatcher.Dispatch(@event); }

            protected CallMatchingHandlersInRegistrationOrderEventDispatcher<TEntityBaseEventInterface>.RegistrationBuilder RegisterEventAppliers()
            {
                return _eventAppliersEventDispatcher.RegisterHandlers();
            }

            public static IReadOnlyEntityCollection<TEntitity, TEntityId> CreateSelfManagingCollection(TRootQueryModel rootQueryModel) => new Collection(rootQueryModel);

            public class Collection : IReadOnlyEntityCollection<TEntitity, TEntityId>
            {
                private readonly TRootQueryModel _aggregate;
                public Collection(TRootQueryModel aggregate)
                {
                    __entities = new EntityCollection<TEntityId, TEntityId>();
                    _aggregate = aggregate;
                    _aggregate.RegisterEventAppliers()
                         .For<TEntityCreatedEventInterface>(
                            e =>
                            {
                                var component = (TEntitity)Activator.CreateInstance(typeof(TEntitity), nonPublic:true);
                                component.RootQueryModel = _aggregate;

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

                private readonly Dictionary<TEntityId, TEntitity> _entities = new Dictionary<TEntityId, TEntitity>();
                private readonly List<TEntitity> _entitiesInCreationOrder = new List<TEntitity>();
                private EntityCollection<TEntityId, TEntityId> __entities;
            }
        }
    }
}
