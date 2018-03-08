using System.Threading.Tasks;
using Composable.Contracts;
using Composable.System.Data.SqlClient;

namespace Composable.Messaging.Buses.Implementation
{
    partial class Inbox
    {
        public partial class MessageStorage
        {
            readonly ISqlConnectionProvider _connectionFactory;

            public MessageStorage(ISqlConnectionProvider connectionFactory) => _connectionFactory = connectionFactory;

            public void SaveIncomingMessage(TransportMessage.InComing message) =>
                _connectionFactory.UseCommand(
                    command =>
                    {
                        command
                            .SetCommandText(
                                $@"
INSERT {InboxMessages.TableName} 
            ({InboxMessages.MessageId},  {InboxMessages.TypeId},  {InboxMessages.Body}, {InboxMessages.Status}) 
    VALUES (@{InboxMessages.MessageId}, @{InboxMessages.TypeId}, @{InboxMessages.Body}, {(int)MessageStatus.UnHandled})
")
                            .AddParameter(InboxMessages.MessageId, message.MessageId)
                            .AddParameter(InboxMessages.TypeId, message.MessageTypeId.GuidValue)
                            //performance: Like with the event store, keep all framework properties out of the JSON and put it into separate columns instead. For events. Reuse a pre-serialized instance from the persisting to the event store.
                            .AddNVarcharMaxParameter(InboxMessages.Body, message.Body)
                            .ExecuteNonQuery();
                    });

            internal void MarkAsHandled(TransportMessage.InComing message) =>
                 _connectionFactory.UseCommand(
                    command =>
                    {
                        var affectedRows = command
                                                 .SetCommandText(
                                                     $@"
UPDATE {InboxMessages.TableName} 
    SET {InboxMessages.Status} = {(int)MessageStatus.Succeeded}
WHERE {InboxMessages.MessageId} = @{InboxMessages.MessageId}
    AND {InboxMessages.Status} = {(int)MessageStatus.UnHandled}
")
                                                 .AddParameter(InboxMessages.MessageId, message.MessageId)
                                                 .ExecuteNonQuery();

                        Assert.Result.Assert(affectedRows == 1);
                        return affectedRows;
                    });

            internal void MarkAsFailed(TransportMessage.InComing message) =>
                _connectionFactory.UseCommand(
                    command =>
                    {
                        var affectedRows = command
                                          .SetCommandText(
                                               $@"
UPDATE {InboxMessages.TableName} 
    SET {InboxMessages.Status} = {(int)MessageStatus.Failed}
WHERE {InboxMessages.MessageId} = @{InboxMessages.MessageId}
    AND {InboxMessages.Status} = {(int)MessageStatus.UnHandled}
")
                                          .AddParameter(InboxMessages.MessageId, message.MessageId)
                                          .ExecuteNonQuery();

                        Assert.Result.Assert(affectedRows == 1);
                        return affectedRows;
                    });

            public async Task StartAsync() => await SchemaManager.EnsureTablesExistAsync(_connectionFactory);
        }
    }
}
