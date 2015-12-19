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


        public abstract class Entity<TComponent, TEntityId, TComponentBaseEventInterface, TComponentCreatedEventInterface, TEventEntityIdGetter>
            where TComponentBaseEventInterface : TAggregateRootBaseEventInterface
            where TComponentCreatedEventInterface : TComponentBaseEventInterface
            where TComponent : Entity<TComponent, TEntityId, TComponentBaseEventInterface, TComponentCreatedEventInterface, TEventEntityIdGetter>
            where TEventEntityIdGetter : IGetAggregateRootEntityEventEntityId<TComponentBaseEventInterface, TEntityId>, new()
        {
            private readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface> _eventAppliersEventDispatcher =
                new CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface>();

            private static readonly TEventEntityIdGetter IdGetter = new TEventEntityIdGetter();

            protected TRootQueryModel RootQueryModel { get; private set; }

            private void ApplyEvent(TComponentBaseEventInterface @event) { _eventAppliersEventDispatcher.Dispatch(@event); }

            protected CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface>.RegistrationBuilder RegisterEventAppliers()
            {
                return _eventAppliersEventDispatcher.RegisterHandlers();
            }

            public static IQueryModelComponentCollection<TComponent, TEntityId> CreateSelfManagingCollection(TRootQueryModel rootQueryModel) => new Collection(rootQueryModel);

            public class Collection : IQueryModelComponentCollection<TComponent, TEntityId>
            {
                private readonly TRootQueryModel _aggregate;
                public Collection(TRootQueryModel aggregate)
                {
                    _aggregate = aggregate;
                    _aggregate.RegisterEventAppliers()
                         .For<TComponentCreatedEventInterface>(
                            e =>
                            {
                                var component = (TComponent)Activator.CreateInstance(typeof(TComponent), nonPublic:true);
                                component.RootQueryModel = _aggregate;

                                _entities.Add(IdGetter.GetId(e), component);
                                _componentsInCreationOrder.Add(component);
                            })
                        .For<TComponentBaseEventInterface>(e => _entities[IdGetter.GetId(e)].ApplyEvent(e));
                }


                public IReadOnlyList<TComponent> InCreationOrder => _componentsInCreationOrder;

                public bool TryGet(TEntityId id, out TComponent component) => _entities.TryGetValue(id, out component);
                public bool Exists(TEntityId id) => _entities.ContainsKey(id);
                public TComponent Get(TEntityId id) => _entities[id];
                public TComponent this[TEntityId id] => _entities[id];

                private readonly Dictionary<TEntityId, TComponent> _entities = new Dictionary<TEntityId, TComponent>();
                private readonly List<TComponent> _componentsInCreationOrder = new List<TComponent>();
            }
        }
    }
}
