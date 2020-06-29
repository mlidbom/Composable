using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;

namespace Composable.Persistence.InMemory.ServiceBus
{
    //urgent: Implement InMemoryOutboxPersistenceLayer
    class InMemoryOutboxPersistenceLayer : IServiceBusPersistenceLayer.IOutboxPersistenceLayer
    {
        public void SaveMessage(IServiceBusPersistenceLayer.OutboxMessageWithReceivers messageWithReceivers) { throw new NotImplementedException(); }
        public int MarkAsReceived(Guid messageId, Guid endpointId) => throw new NotImplementedException();
        public Task InitAsync() => throw new NotImplementedException();
    }
}
