using Composable.Messaging.Events;

namespace Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels
{
    public abstract partial class SelfGeneratingQueryModel<TAggregate,  TAggregateBaseEventInterface>
        where TAggregate : SelfGeneratingQueryModel<TAggregate,  TAggregateBaseEventInterface>
        where TAggregateBaseEventInterface : class, IAggregateRootEvent
    {
        public abstract partial class Component<TComponent, TComponentBaseEventInterface>
            where TComponentBaseEventInterface : class, TAggregateBaseEventInterface
            where TComponent : Component<TComponent, TComponentBaseEventInterface>
        {
            readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface> _eventAppliersEventDispatcher =
                new CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface>();

            void ApplyEvent(TComponentBaseEventInterface @event)
            {
                _eventAppliersEventDispatcher.Dispatch(@event);
            }

            protected Component(TAggregate aggregateRoot)
                : this(
                    appliersRegistrar: aggregateRoot.RegisterEventAppliers(),
                    registerEventAppliers: true)
            {}

            internal Component(IEventHandlerRegistrar<TComponentBaseEventInterface> appliersRegistrar, bool registerEventAppliers)
            {
                if(registerEventAppliers)
                {
                    appliersRegistrar
                                 .For<TComponentBaseEventInterface>(ApplyEvent);
                }
            }

            protected IEventHandlerRegistrar<TComponentBaseEventInterface> RegisterEventAppliers() => _eventAppliersEventDispatcher.Register();
        }
    }
}
