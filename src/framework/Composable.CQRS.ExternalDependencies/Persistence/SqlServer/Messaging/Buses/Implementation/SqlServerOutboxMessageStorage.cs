using System.Linq;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.SqlServer.SystemExtensions;
using Composable.Refactoring.Naming;
using Composable.Serialization;

namespace Composable.Persistence.SqlServer.Messaging.Buses.Implementation
{
    //urgent: separate out persistence code from this into IServiceBusPersistenceLayer.IOutboxStorage or something similar
    partial class SqlServerOutboxMessageStorage : Outbox.IMessageStorage
    {
        readonly ISqlServerConnectionProvider _connectionFactory;
        readonly IServiceBusPersistenceLayer.IOutboxPersistenceLayer _persistenceLayer;
        readonly ITypeMapper _typeMapper;
        readonly IRemotableMessageSerializer _serializer;

        public SqlServerOutboxMessageStorage(ISqlServerConnectionProvider connectionFactory, IServiceBusPersistenceLayer.IOutboxPersistenceLayer persistenceLayer, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
        {
            _connectionFactory = connectionFactory;
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

        public async Task StartAsync() => await SchemaManager.EnsureTablesExistAsync(_connectionFactory);
    }
}
