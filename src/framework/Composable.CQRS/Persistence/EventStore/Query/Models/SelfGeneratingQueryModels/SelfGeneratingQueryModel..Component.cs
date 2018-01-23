using Composable.Messaging.Events;

namespace Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels
{
    public abstract partial class SelfGeneratingQueryModel<TAggregateRoot,  TAggregateRootBaseEventInterface>
        where TAggregateRoot : SelfGeneratingQueryModel<TAggregateRoot,  TAggregateRootBaseEventInterface>
        where TAggregateRootBaseEventInterface : class, IAggregateRootEvent
    {
        public abstract partial class Component<TComponent, TComponentBaseEventInterface>
            where TComponentBaseEventInterface : class, TAggregateRootBaseEventInterface
            where TComponent : Component<TComponent, TComponentBaseEventInterface>
        {
            readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface> _eventAppliersEventDispatcher =
                new CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface>();

            void ApplyEvent(TComponentBaseEventInterface @event)
            {
                _eventAppliersEventDispatcher.Dispatch(@event);
            }

            protected Component(TAggregateRoot aggregateRoot)
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
