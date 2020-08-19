using System;
using System.Linq;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.TasksCE;

namespace Composable.Messaging.Buses.Implementation
{
    partial class Outbox
    {
        internal class MessageStorage : Outbox.IMessageStorage
        {
            readonly IServiceBusPersistenceLayer.IOutboxPersistenceLayer _persistenceLayer;
            readonly ITypeMapper _typeMapper;
            readonly IRemotableMessageSerializer _serializer;

            public MessageStorage(IServiceBusPersistenceLayer.IOutboxPersistenceLayer persistenceLayer, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
            {
                _persistenceLayer = persistenceLayer;
                _typeMapper = typeMapper;
                _serializer = serializer;
            }

            public void SaveMessage(IExactlyOnceMessage message, params EndpointId[] receiverEndpointIds)
            {
                var outboxMessageWithReceivers = new IServiceBusPersistenceLayer.OutboxMessageWithReceivers(_serializer.SerializeMessage(message),
                                                                                                            _typeMapper.GetId(message.GetType()).GuidValue,
                                                                                                            message.MessageId,
                                                                                                            receiverEndpointIds.Select(@this => @this.GuidValue));

                _persistenceLayer.SaveMessage(outboxMessageWithReceivers);
            }

            public void MarkAsReceived(Guid messageId, EndpointId receiverId)
            {
                var endpointIdGuidValue = receiverId.GuidValue;
                var affectedRows = _persistenceLayer.MarkAsReceived(messageId, endpointIdGuidValue);
                Assert.Result.Assert(affectedRows == 1);
            }

            public async Task StartAsync() => await _persistenceLayer.InitAsync().NoMarshalling();
        }
    }
}
