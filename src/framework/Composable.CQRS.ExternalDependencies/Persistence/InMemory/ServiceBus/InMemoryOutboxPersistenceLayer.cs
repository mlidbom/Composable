using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.System.Collections.Collections;
using Composable.System.Linq;
using Composable.System.Threading.ResourceAccess;
using Message = Composable.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.OutboxMessageWithReceivers;

namespace Composable.Persistence.InMemory.ServiceBus
{
    //urgent: Handle transactions correctly. Consider using SqlLite in-memory instead: https://www.sqlite.org/inmemorydb.html
    class InMemoryOutboxPersistenceLayer : IServiceBusPersistenceLayer.IOutboxPersistenceLayer
    {
        readonly OptimizedThreadShared<Implementation> _implementation = new OptimizedThreadShared<Implementation>(new Implementation());

        public void SaveMessage(Message messageWithReceivers) => _implementation.WithExclusiveAccess(@this => @this.SaveMessage(messageWithReceivers));
        public int MarkAsReceived(Guid messageId, Guid endpointId) => _implementation.WithExclusiveAccess(@this => @this.MarkAsReceived(messageId, endpointId));
        public Task InitAsync() => _implementation.WithExclusiveAccess(@this => @this.InitAsync());

        class Implementation : IServiceBusPersistenceLayer.IOutboxPersistenceLayer
        {
            readonly List<Message> _messages = new List<Message>();
            readonly Dictionary<Guid, Dictionary<Guid, bool>> _dispatchingStatus = new Dictionary<Guid, Dictionary<Guid, bool>>();

            public void SaveMessage(Message messageWithReceivers)
            {
                _messages.Add(messageWithReceivers);
                var dispatchingInfo =_dispatchingStatus.GetOrAdd(messageWithReceivers.MessageId, () => new Dictionary<Guid, bool>());
                messageWithReceivers.ReceiverEndpointIds.ForEach(@this => dispatchingInfo[@this] = false);
            }

            public int MarkAsReceived(Guid messageId, Guid endpointId)
            {
                _dispatchingStatus[messageId][endpointId] = true;
                return 1;
            }

            public Task InitAsync() => Task.CompletedTask;
        }
    }
}
