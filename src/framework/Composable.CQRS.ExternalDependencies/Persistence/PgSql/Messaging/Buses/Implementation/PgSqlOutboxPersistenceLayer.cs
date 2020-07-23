using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.PgSql.SystemExtensions;
using Composable.SystemCE.Linq;
using NpgsqlTypes;
using MessageTable = Composable.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.OutboxMessagesDatabaseSchemaStrings;
using DispatchingTable = Composable.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.OutboxMessageDispatchingTableSchemaStrings;

namespace Composable.Persistence.PgSql.Messaging.Buses.Implementation
{
    partial class PgSqlOutboxPersistenceLayer : IServiceBusPersistenceLayer.IOutboxPersistenceLayer
    {
        readonly INpgsqlConnectionProvider _connectionFactory;
        public PgSqlOutboxPersistenceLayer(INpgsqlConnectionProvider connectionFactory) => _connectionFactory = connectionFactory;

        public void SaveMessage(IServiceBusPersistenceLayer.OutboxMessageWithReceivers messageWithReceivers)
        {
            _connectionFactory.UseCommand(
                command =>
                {
                    command
                       .SetCommandText(
                            $@"
INSERT INTO {MessageTable.TableName} 
            ({MessageTable.MessageId},  {MessageTable.TypeIdGuidValue}, {MessageTable.SerializedMessage}) 
    VALUES (@{MessageTable.MessageId}, @{MessageTable.TypeIdGuidValue}, @{MessageTable.SerializedMessage});
")
                       .AddParameter(MessageTable.MessageId, messageWithReceivers.MessageId)
                       .AddParameter(MessageTable.TypeIdGuidValue, messageWithReceivers.TypeIdGuidValue)
                        //performance: Like with the event store, keep all framework properties out of the JSON and put it into separate columns instead. For events. Reuse a pre-serialized instance from the persisting to the event store.
                       .AddMediumTextParameter(MessageTable.SerializedMessage, messageWithReceivers.SerializedMessage)
                       .AddParameter(DispatchingTable.IsReceived, NpgsqlDbType.Boolean, false);

                    messageWithReceivers.ReceiverEndpointIds.ForEach(
                        (endpointId, index)
                            => command.AppendCommandText($@"
INSERT INTO {DispatchingTable.TableName} 
            ({DispatchingTable.MessageId},  {DispatchingTable.EndpointId},          {DispatchingTable.IsReceived}) 
    VALUES (@{DispatchingTable.MessageId}, @{DispatchingTable.EndpointId}_{index}, @{DispatchingTable.IsReceived});
").AddParameter($"{DispatchingTable.EndpointId}_{index}", endpointId));

                    command.ExecuteNonQuery();
                });
        }

        public int MarkAsReceived(Guid messageId, Guid endpointId)
        {
            return _connectionFactory.UseCommand(
                command => command
                          .SetCommandText(
                               $@"
UPDATE {DispatchingTable.TableName} 
    SET {DispatchingTable.IsReceived} = true
WHERE {DispatchingTable.MessageId} = @{DispatchingTable.MessageId}
    AND {DispatchingTable.EndpointId} = @{DispatchingTable.EndpointId}
    AND {DispatchingTable.IsReceived} = false;
")
                          .AddParameter(DispatchingTable.MessageId, messageId)
                          .AddParameter(DispatchingTable.EndpointId, endpointId)
                          .ExecuteNonQuery());
        }

        public Task InitAsync() => SchemaManager.EnsureTablesExistAsync(_connectionFactory);
    }
}
