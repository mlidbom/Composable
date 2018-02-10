using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.EventStore;
using Composable.SystemExtensions.Threading;
using JetBrains.Annotations;

namespace Composable.Messaging.Buses
{
    [UsedImplicitly] class EventstoreEventPublisher : IEventstoreEventPublisher
    {
        readonly IInterprocessTransport _transport;
        readonly IMessageHandlerRegistry _handlerRegistry;

        public EventstoreEventPublisher(IInterprocessTransport transport, IMessageHandlerRegistry handlerRegistry)
        {
            _transport = transport;
            _handlerRegistry = handlerRegistry;
        }

        void IEventstoreEventPublisher.Publish(IAggregateEvent @event)
        {
            MessageInspector.AssertValidToSendRemote(@event);
            _handlerRegistry.CreateEventDispatcher().Dispatch(@event);
            _transport.DispatchIfTransactionCommits(@event);
        }
    }
}
