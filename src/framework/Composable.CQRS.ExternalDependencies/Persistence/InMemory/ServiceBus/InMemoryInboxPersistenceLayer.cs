using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;

namespace Composable.Persistence.InMemory.ServiceBus
{
    //urgent: Implement InMemoryInboxPersistenceLayer 
    class InMemoryInboxPersistenceLayer : IServiceBusPersistenceLayer.IInboxPersistenceLayer
    {
        public void SaveMessage(Guid messageId, Guid typeId, string serializedMessage) { throw new NotImplementedException(); }
        public void MarkAsSucceeded(Guid messageId) { throw new NotImplementedException(); }
        public int RecordException(Guid messageId, string exceptionStackTrace, string exceptionMessage, string exceptionType) => throw new NotImplementedException();
        public int MarkAsFailed(Guid messageId) => throw new NotImplementedException();
        public Task InitAsync() => throw new NotImplementedException();
    }
}
