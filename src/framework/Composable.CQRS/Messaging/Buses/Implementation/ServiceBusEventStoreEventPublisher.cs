using Composable.Persistence.EventStore;
using JetBrains.Annotations;

namespace Composable.Messaging.Buses.Implementation
{
    [UsedImplicitly] class ServiceBusEventStoreEventPublisher : IEventStoreEventPublisher
    {
        readonly IOutbox _transport;
        readonly IMessageHandlerRegistry _handlerRegistry;

        public ServiceBusEventStoreEventPublisher(IOutbox transport, IMessageHandlerRegistry handlerRegistry)
        {
            _transport = transport;
            _handlerRegistry = handlerRegistry;
        }

        void IEventStoreEventPublisher.Publish(IAggregateEvent @event)
        {
            MessageInspector.AssertValidToSendRemote(@event);
            _handlerRegistry.CreateEventDispatcher().Dispatch(@event);
            _transport.PublishTransactionally(@event);
        }
    }
}
