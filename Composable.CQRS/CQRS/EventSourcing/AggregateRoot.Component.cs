using System;
using Composable.CQRS.EventSourcing;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Events;

namespace Composable.CQRS.CQRS.EventSourcing
{
    public abstract partial class AggregateRoot<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>
        where TAggregateRoot : AggregateRoot<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>
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
            readonly Action<TComponentBaseEventClass> _raiseEventThroughParent;

            IUtcTimeTimeSource TimeSource { get; set; }

            void ApplyEvent(TComponentBaseEventInterface @event)
            {
                _eventAppliersEventDispatcher.Dispatch(@event);
            }

            protected Component(TAggregateRoot aggregateRoot)
                : this(
                    timeSource: aggregateRoot.TimeSource,
                    raiseEventThroughParent: aggregateRoot.RaiseEvent,
                    appliersRegistrar: aggregateRoot.RegisterEventAppliers(),
                    registerEventAppliers: true)
            {}

            internal Component(IUtcTimeTimeSource timeSource, Action<TComponentBaseEventClass> raiseEventThroughParent, IEventHandlerRegistrar<TComponentBaseEventInterface> appliersRegistrar, bool registerEventAppliers)
            {
                TimeSource = timeSource;
                _raiseEventThroughParent = raiseEventThroughParent;
                _eventHandlersEventDispatcher.Register()
                                            .IgnoreUnhandled<TComponentBaseEventInterface>();

                if(registerEventAppliers)
                {
                    appliersRegistrar
                                 .For<TComponentBaseEventInterface>(ApplyEvent);
                }
            }

            protected virtual void RaiseEvent(TComponentBaseEventClass @event)
            {
                _raiseEventThroughParent(@event);
                _eventHandlersEventDispatcher.Dispatch(@event);
            }

            protected IEventHandlerRegistrar<TComponentBaseEventInterface> RegisterEventAppliers()
            {
                return _eventAppliersEventDispatcher.Register();
            }

            // ReSharper disable once UnusedMember.Global todo: tests
            protected IEventHandlerRegistrar<TComponentBaseEventInterface> RegisterEventHandlers()
            {
                return _eventHandlersEventDispatcher.Register();
            }
        }
    }
}
