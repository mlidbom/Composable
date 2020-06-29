using System.Linq;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.Refactoring.Naming;
using Composable.Serialization;

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

            public void SaveMessage(MessageTypes.Remotable.ExactlyOnce.IMessage message, params EndpointId[] receiverEndpointIds)
            {
                var outboxMessageWithReceivers = new IServiceBusPersistenceLayer.OutboxMessageWithReceivers(_serializer.SerializeMessage(message),
                                                                                                            _typeMapper.GetId(message.GetType()).GuidValue,
                                                                                                            message.MessageId,
                                                                                                            receiverEndpointIds.Select(@this => @this.GuidValue));

                _persistenceLayer.SaveMessage(outboxMessageWithReceivers);
            }

            public void MarkAsReceived(TransportMessage.Response.Incoming response, EndpointId endpointId)
            {
                var endpointIdGuidValue = endpointId.GuidValue;
                var responseRespondingToMessageId = response.RespondingToMessageId;
                var affectedRows = _persistenceLayer.MarkAsReceived(responseRespondingToMessageId, endpointIdGuidValue);
                Assert.Result.Assert(affectedRows == 1);
            }

            public async Task StartAsync() => await _persistenceLayer.InitAsync();
        }
    }
}
