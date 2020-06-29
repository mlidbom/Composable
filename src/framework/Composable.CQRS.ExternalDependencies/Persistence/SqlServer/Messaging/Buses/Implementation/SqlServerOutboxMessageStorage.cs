using System.Threading.Tasks;
using Composable.Contracts;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.SqlServer.SystemExtensions;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.System.Linq;
using MessageTable = Composable.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.OutboxMessagesDatabaseSchemaStrings;
using DispatchingTable = Composable.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.OutboxMessageDispatchingTableSchemaStrings;
namespace Composable.Persistence.SqlServer.Messaging.Buses.Implementation
{
    //urgent: separate out persistence code from this into IServiceBusPersistenceLayer.IOutboxStorage or something similar
    partial class SqlServerOutboxMessageStorage : Outbox.IMessageStorage
    {
        readonly ISqlServerConnectionProvider _connectionFactory;
        readonly ITypeMapper _typeMapper;
        readonly IRemotableMessageSerializer _serializer;

        public SqlServerOutboxMessageStorage(ISqlServerConnectionProvider connectionFactory, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
        {
            _connectionFactory = connectionFactory;
            _typeMapper = typeMapper;
            _serializer = serializer;
        }

        public void SaveMessage(MessageTypes.Remotable.ExactlyOnce.IMessage message, params EndpointId[] receiverEndpointIds) =>
            _connectionFactory.UseCommand(
                command =>
                {
                    command
                       .SetCommandText(
                            $@"
INSERT {MessageTable.TableName} 
            ({MessageTable.MessageId},  {MessageTable.TypeIdGuidValue}, {MessageTable.Body}) 
    VALUES (@{MessageTable.MessageId}, @{MessageTable.TypeIdGuidValue}, @{MessageTable.Body})
")
                       .AddParameter(MessageTable.MessageId, message.DeduplicationId)
                       .AddParameter(MessageTable.TypeIdGuidValue, _typeMapper.GetId(message.GetType()).GuidValue)
                        //performance: Like with the event store, keep all framework properties out of the JSON and put it into separate columns instead. For events. Reuse a pre-serialized instance from the persisting to the event store.
                       .AddNVarcharMaxParameter(MessageTable.Body, _serializer.SerializeMessage(message))
                       .AddParameter(DispatchingTable.IsReceived, 0);

                    receiverEndpointIds.ForEach(
                        (endpointId, index)
                            => SqlCommandParameterExtensions.AddParameter(command.AppendCommandText(
                                                                              $@"
INSERT {DispatchingTable.TableName} 
            ({DispatchingTable.MessageId},  {DispatchingTable.EndpointId},          {DispatchingTable.IsReceived}) 
    VALUES (@{DispatchingTable.MessageId}, @{DispatchingTable.EndpointId}_{index}, @{DispatchingTable.IsReceived})
"), $"{DispatchingTable.EndpointId}_{index}" as string, endpointId.GuidValue));

                    command.ExecuteNonQuery();
                });

        public void MarkAsReceived(TransportMessage.Response.Incoming response, EndpointId endpointId) =>
            _connectionFactory.UseCommand(
                command =>
                {
                    var affectedRows = command
                                      .SetCommandText(
                                           $@"
UPDATE {DispatchingTable.TableName} 
    SET {DispatchingTable.IsReceived} = 1
WHERE {DispatchingTable.MessageId} = @{DispatchingTable.MessageId}
    AND {DispatchingTable.EndpointId} = @{DispatchingTable.EndpointId}
    AND {DispatchingTable.IsReceived} = 0
")
                                      .AddParameter(DispatchingTable.MessageId, response.RespondingToMessageId)
                                      .AddParameter(DispatchingTable.EndpointId, endpointId.GuidValue)
                                      .ExecuteNonQuery();

                    Assert.Result.Assert(affectedRows == 1);
                    return affectedRows;
                });

        public async Task StartAsync() => await SchemaManager.EnsureTablesExistAsync(_connectionFactory);
    }
}
