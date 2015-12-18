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
        public abstract class NestedEntity<TComponent, TComponentBaseEventClass, TComponentBaseEventInterface, TComponentCreatedEventInterface>
            where TComponentBaseEventInterface : TAggregateRootBaseEventInterface, IAggregateRootComponentEvent
            where TComponentBaseEventClass : TAggregateRootBaseEventClass, TComponentBaseEventInterface
            where TComponentCreatedEventInterface : TComponentBaseEventInterface, IAggregateRootComponentCreatedEvent
            where TComponent : NestedEntity<TComponent, TComponentBaseEventClass, TComponentBaseEventInterface, TComponentCreatedEventInterface>
        {
            private readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface> _eventAppliersEventDispatcher =
                new CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface>();
            private readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface> _eventHandlersEventDispatcher =
                new CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface>();

            protected IUtcTimeTimeSource TimeSource => AggregateRoot.TimeSource;
            protected TAggregateRoot AggregateRoot { get; private set; }

            private void ApplyEvent(TComponentBaseEventInterface @event) { _eventAppliersEventDispatcher.Dispatch(@event); }

            protected NestedEntity()
            {
                _eventHandlersEventDispatcher.Register()
                                             .IgnoreUnhandled<TComponentBaseEventInterface>();
            }

            protected void RaiseEvent(TComponentBaseEventClass @event)
            {
                AggregateRoot.RaiseEvent(@event);
                _eventHandlersEventDispatcher.Dispatch(@event);
            }

            protected CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface>.RegistrationBuilder RegisterEventAppliers()
            {
                return _eventAppliersEventDispatcher.RegisterHandlers();
            }

            protected CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface>.RegistrationBuilder RegisterEventHandlers()
            {
                return _eventHandlersEventDispatcher.RegisterHandlers();
            }


            public static Collection CreateSelfManagingCollection(TAggregateRoot aggregate) => new Collection(aggregate);

            public class Collection : IReadOnlyAggregateRootComponentCollection<TComponent>
            {
                private readonly TAggregateRoot _aggregate;
                public Collection(TAggregateRoot aggregate)
                {
                    _aggregate = aggregate;
                    _aggregate.RegisterEventAppliers()
                        .For<TComponentCreatedEventInterface>(
                            e =>
                            {
                                var component = (TComponent)Activator.CreateInstance(typeof(TComponent), nonPublic: true);
                                component.AggregateRoot = _aggregate;

                                _components.Add(e.ComponentId, component);
                                _componentsInCreationOrder.Add(component);
                            })
                        .For<TComponentBaseEventInterface>(e => _components[e.ComponentId].ApplyEvent(e));
                }

   

                public TComponent Add<TCreationEvent>(TCreationEvent creationEvent) where TCreationEvent : TComponentBaseEventClass, TComponentCreatedEventInterface
                {
                    _aggregate.RaiseEvent(creationEvent);
                    var result = _componentsInCreationOrder.Last();
                    result._eventHandlersEventDispatcher.Dispatch(creationEvent);
                    return result;
                }

                public IReadOnlyList<TComponent> InCreationOrder => _componentsInCreationOrder;

                public bool TryGet(Guid id, out TComponent component) => _components.TryGetValue(id, out component);
                [Pure]
                public bool Exists(Guid id) => _components.ContainsKey(id);
                public TComponent Get(Guid id) => _components[id];


                private readonly Dictionary<Guid, TComponent> _components = new Dictionary<Guid, TComponent>();
                private readonly List<TComponent> _componentsInCreationOrder = new List<TComponent>();
            }
        }

    }
}
