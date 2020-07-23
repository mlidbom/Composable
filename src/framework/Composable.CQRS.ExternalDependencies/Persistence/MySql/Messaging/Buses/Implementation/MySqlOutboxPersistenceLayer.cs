using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.MySql.SystemExtensions;
using Composable.SystemCE.Linq;
using MessageTable = Composable.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.OutboxMessagesDatabaseSchemaStrings;
using DispatchingTable = Composable.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.OutboxMessageDispatchingTableSchemaStrings;

namespace Composable.Persistence.MySql.Messaging.Buses.Implementation
{
    partial class MySqlOutboxPersistenceLayer : IServiceBusPersistenceLayer.IOutboxPersistenceLayer
    {
        readonly IMySqlConnectionProvider _connectionFactory;
        public MySqlOutboxPersistenceLayer(IMySqlConnectionProvider connectionFactory) => _connectionFactory = connectionFactory;

        public void SaveMessage(IServiceBusPersistenceLayer.OutboxMessageWithReceivers messageWithReceivers)
        {
            _connectionFactory.UseCommand(
                command =>
                {
                    command
                       .SetCommandText(
                            $@"
INSERT {MessageTable.TableName} 
            ({MessageTable.MessageId},  {MessageTable.TypeIdGuidValue}, {MessageTable.SerializedMessage}) 
    VALUES (@{MessageTable.MessageId}, @{MessageTable.TypeIdGuidValue}, @{MessageTable.SerializedMessage});
")
                       .AddParameter(MessageTable.MessageId, messageWithReceivers.MessageId)
                       .AddParameter(MessageTable.TypeIdGuidValue, messageWithReceivers.TypeIdGuidValue)
                        //performance: Like with the event store, keep all framework properties out of the JSON and put it into separate columns instead. For events. Reuse a pre-serialized instance from the persisting to the event store.
                       .AddMediumTextParameter(MessageTable.SerializedMessage, messageWithReceivers.SerializedMessage)
                       .AddParameter(DispatchingTable.IsReceived, 0);

                    messageWithReceivers.ReceiverEndpointIds.ForEach(
                        (endpointId, index)
                            => command.AppendCommandText($@"
INSERT {DispatchingTable.TableName} 
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
    SET {DispatchingTable.IsReceived} = 1
WHERE {DispatchingTable.MessageId} = @{DispatchingTable.MessageId}
    AND {DispatchingTable.EndpointId} = @{DispatchingTable.EndpointId}
    AND {DispatchingTable.IsReceived} = 0
")
                          .AddParameter(DispatchingTable.MessageId, messageId)
                          .AddParameter(DispatchingTable.EndpointId, endpointId)
                          .ExecuteNonQuery());
        }

        public Task InitAsync() => SchemaManager.EnsureTablesExistAsync(_connectionFactory);
    }
}
