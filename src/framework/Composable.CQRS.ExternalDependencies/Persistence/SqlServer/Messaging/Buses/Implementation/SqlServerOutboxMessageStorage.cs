using System.Threading.Tasks;
using Composable.Contracts;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.SqlServer.SystemExtensions;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.System.Linq;

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
INSERT {OutboxMessagesDatabaseSchemaStrings.TableName} 
            ({OutboxMessagesDatabaseSchemaStrings.MessageId},  {OutboxMessagesDatabaseSchemaStrings.TypeIdGuidValue}, {OutboxMessagesDatabaseSchemaStrings.Body}) 
    VALUES (@{OutboxMessagesDatabaseSchemaStrings.MessageId}, @{OutboxMessagesDatabaseSchemaStrings.TypeIdGuidValue}, @{OutboxMessagesDatabaseSchemaStrings.Body})
")
                       .AddParameter(OutboxMessagesDatabaseSchemaStrings.MessageId, message.DeduplicationId)
                       .AddParameter(OutboxMessagesDatabaseSchemaStrings.TypeIdGuidValue, _typeMapper.GetId(message.GetType()).GuidValue)
                        //performance: Like with the event store, keep all framework properties out of the JSON and put it into separate columns instead. For events. Reuse a pre-serialized instance from the persisting to the event store.
                       .AddNVarcharMaxParameter(OutboxMessagesDatabaseSchemaStrings.Body, _serializer.SerializeMessage(message))
                       .AddParameter(OutboxMessageDispatchingTableSchemaStrings.IsReceived, 0);

                    receiverEndpointIds.ForEach(
                        (endpointId, index)
                            => SqlCommandParameterExtensions.AddParameter(command.AppendCommandText(
                                                                              $@"
INSERT {OutboxMessageDispatchingTableSchemaStrings.TableName} 
            ({OutboxMessageDispatchingTableSchemaStrings.MessageId},  {OutboxMessageDispatchingTableSchemaStrings.EndpointId},          {OutboxMessageDispatchingTableSchemaStrings.IsReceived}) 
    VALUES (@{OutboxMessageDispatchingTableSchemaStrings.MessageId}, @{OutboxMessageDispatchingTableSchemaStrings.EndpointId}_{index}, @{OutboxMessageDispatchingTableSchemaStrings.IsReceived})
"), (string)$"{OutboxMessageDispatchingTableSchemaStrings.EndpointId}_{index}", endpointId.GuidValue));

                    command.ExecuteNonQuery();
                });

        public void MarkAsReceived(TransportMessage.Response.Incoming response, EndpointId endpointId) =>
            _connectionFactory.UseCommand(
                command =>
                {
                    var affectedRows = command
                                      .SetCommandText(
                                           $@"
UPDATE {OutboxMessageDispatchingTableSchemaStrings.TableName} 
    SET {OutboxMessageDispatchingTableSchemaStrings.IsReceived} = 1
WHERE {OutboxMessageDispatchingTableSchemaStrings.MessageId} = @{OutboxMessageDispatchingTableSchemaStrings.MessageId}
    AND {OutboxMessageDispatchingTableSchemaStrings.EndpointId} = @{OutboxMessageDispatchingTableSchemaStrings.EndpointId}
    AND {OutboxMessageDispatchingTableSchemaStrings.IsReceived} = 0
")
                                      .AddParameter(OutboxMessageDispatchingTableSchemaStrings.MessageId, response.RespondingToMessageId)
                                      .AddParameter(OutboxMessageDispatchingTableSchemaStrings.EndpointId, endpointId.GuidValue)
                                      .ExecuteNonQuery();

                    Assert.Result.Assert(affectedRows == 1);
                    return affectedRows;
                });

        public async Task StartAsync() => await SchemaManager.EnsureTablesExistAsync(_connectionFactory);
    }
}
