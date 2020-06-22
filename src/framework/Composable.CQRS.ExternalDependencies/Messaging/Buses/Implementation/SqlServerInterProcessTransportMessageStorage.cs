using System;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.System.Data.SqlClient;
using Composable.System.Linq;

namespace Composable.Messaging.Buses.Implementation
{
    partial class SqlServerInterProcessTransportMessageStorage : InterprocessTransport.IMessageStorage
    {
        readonly ISqlConnectionProvider _connectionFactory;
        readonly ITypeMapper _typeMapper;
        readonly IRemotableMessageSerializer _serializer;

        public SqlServerInterProcessTransportMessageStorage(ISqlConnectionProvider connectionFactory, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
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
INSERT {InterprocessTransportOutboxMessagesDatabaseSchemaStrings.TableName} 
            ({InterprocessTransportOutboxMessagesDatabaseSchemaStrings.MessageId},  {InterprocessTransportOutboxMessagesDatabaseSchemaStrings.TypeIdGuidValue}, {InterprocessTransportOutboxMessagesDatabaseSchemaStrings.Body}) 
    VALUES (@{InterprocessTransportOutboxMessagesDatabaseSchemaStrings.MessageId}, @{InterprocessTransportOutboxMessagesDatabaseSchemaStrings.TypeIdGuidValue}, @{InterprocessTransportOutboxMessagesDatabaseSchemaStrings.Body})
")
                       .AddParameter(InterprocessTransportOutboxMessagesDatabaseSchemaStrings.MessageId, message.DeduplicationId)
                       .AddParameter(InterprocessTransportOutboxMessagesDatabaseSchemaStrings.TypeIdGuidValue, _typeMapper.GetId(message.GetType()).GuidValue)
                        //performance: Like with the event store, keep all framework properties out of the JSON and put it into separate columns instead. For events. Reuse a pre-serialized instance from the persisting to the event store.
                       .AddNVarcharMaxParameter(InterprocessTransportOutboxMessagesDatabaseSchemaStrings.Body, _serializer.SerializeMessage(message))
                       .AddParameter(InterprocessTransportMessageDispatchingDatabaseSchemaNames.IsReceived, 0);

                    receiverEndpointIds.ForEach(
                        (endpointId, index)
                            => SqlCommandParameterExtensions.AddParameter(command.AppendCommandText(
                                                                              $@"
INSERT {InterprocessTransportMessageDispatchingDatabaseSchemaNames.TableName} 
            ({InterprocessTransportMessageDispatchingDatabaseSchemaNames.MessageId},  {InterprocessTransportMessageDispatchingDatabaseSchemaNames.EndpointId},          {InterprocessTransportMessageDispatchingDatabaseSchemaNames.IsReceived}) 
    VALUES (@{InterprocessTransportMessageDispatchingDatabaseSchemaNames.MessageId}, @{InterprocessTransportMessageDispatchingDatabaseSchemaNames.EndpointId}_{index}, @{InterprocessTransportMessageDispatchingDatabaseSchemaNames.IsReceived})
"), (string)$"{InterprocessTransportMessageDispatchingDatabaseSchemaNames.EndpointId}_{index}", (Guid)endpointId.GuidValue));

                    command.ExecuteNonQuery();
                });

        public void MarkAsReceived(TransportMessage.Response.Incoming response, EndpointId endpointId) =>
            _connectionFactory.UseCommand(
                command =>
                {
                    var affectedRows = command
                                      .SetCommandText(
                                           $@"
UPDATE {InterprocessTransportMessageDispatchingDatabaseSchemaNames.TableName} 
    SET {InterprocessTransportMessageDispatchingDatabaseSchemaNames.IsReceived} = 1
WHERE {InterprocessTransportMessageDispatchingDatabaseSchemaNames.MessageId} = @{InterprocessTransportMessageDispatchingDatabaseSchemaNames.MessageId}
    AND {InterprocessTransportMessageDispatchingDatabaseSchemaNames.EndpointId} = @{InterprocessTransportMessageDispatchingDatabaseSchemaNames.EndpointId}
    AND {InterprocessTransportMessageDispatchingDatabaseSchemaNames.IsReceived} = 0
")
                                      .AddParameter(InterprocessTransportMessageDispatchingDatabaseSchemaNames.MessageId, response.RespondingToMessageId)
                                      .AddParameter(InterprocessTransportMessageDispatchingDatabaseSchemaNames.EndpointId, endpointId.GuidValue)
                                      .ExecuteNonQuery();

                    Assert.Result.Assert(affectedRows == 1);
                    return affectedRows;
                });

        public async Task StartAsync() => await SchemaManager.EnsureTablesExistAsync(_connectionFactory);
    }
}
