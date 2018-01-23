using Composable.GenericAbstractions.Time;
using Composable.Messaging.Events;

namespace Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels
{
    public abstract partial class SelfGeneratingQueryModel<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>
        where TAggregateRoot : SelfGeneratingQueryModel<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>
        where TAggregateRootBaseEventInterface : class, IAggregateRootEvent
        where TAggregateRootBaseEventClass : AggregateRootEvent, TAggregateRootBaseEventInterface
    {
        public abstract partial class Component<TComponent, TComponentBaseEventClass, TComponentBaseEventInterface>
            where TComponentBaseEventInterface : class, TAggregateRootBaseEventInterface
            where TComponentBaseEventClass : TAggregateRootBaseEventClass, TComponentBaseEventInterface
            where TComponent : Component<TComponent, TComponentBaseEventClass, TComponentBaseEventInterface>
        {
            readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface> _eventAppliersEventDispatcher =
                new CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface>();
            readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface> _eventHandlersEventDispatcher =
                new CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentBaseEventInterface>();

            IUtcTimeTimeSource TimeSource { get; set; }

            void ApplyEvent(TComponentBaseEventInterface @event)
            {
                _eventAppliersEventDispatcher.Dispatch(@event);
            }

            protected Component(TAggregateRoot aggregateRoot)
                : this(
                    timeSource: aggregateRoot.TimeSource,
                    appliersRegistrar: aggregateRoot.RegisterEventAppliers(),
                    registerEventAppliers: true)
            {}

            internal Component(IUtcTimeTimeSource timeSource, IEventHandlerRegistrar<TComponentBaseEventInterface> appliersRegistrar, bool registerEventAppliers)
            {
                TimeSource = timeSource;
                _eventHandlersEventDispatcher.Register()
                                            .IgnoreUnhandled<TComponentBaseEventInterface>();

                if(registerEventAppliers)
                {
                    appliersRegistrar
                                 .For<TComponentBaseEventInterface>(ApplyEvent);
                }
            }

            protected IEventHandlerRegistrar<TComponentBaseEventInterface> RegisterEventAppliers() => _eventAppliersEventDispatcher.Register();

            // ReSharper disable once UnusedMember.Global todo: tests
            protected IEventHandlerRegistrar<TComponentBaseEventInterface> RegisterEventHandlers() => _eventHandlersEventDispatcher.Register();
        }
    }
}
