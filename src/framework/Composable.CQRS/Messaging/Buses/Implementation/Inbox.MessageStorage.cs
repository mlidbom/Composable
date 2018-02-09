using System.Threading.Tasks;
using Composable.Contracts;
using Composable.System.Data.SqlClient;

namespace Composable.Messaging.Buses.Implementation
{
    partial class Inbox
    {
        public partial class MessageStorage
        {
            readonly ISqlConnection _connectionFactory;

            public MessageStorage(ISqlConnection connectionFactory) => _connectionFactory = connectionFactory;

            public void SaveMessage(TransportMessage.InComing message) =>
                _connectionFactory.UseCommand(
                    command =>
                    {
                        command
                            .SetCommandText(
                                $@"
INSERT {InboxMessages.TableName} 
            ({InboxMessages.MessageId},  {InboxMessages.TypeId},  {InboxMessages.Body}, {InboxMessages.IsHandled}) 
    VALUES (@{InboxMessages.MessageId}, @{InboxMessages.TypeId}, @{InboxMessages.Body}, 0)
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
    SET {InboxMessages.IsHandled} = 1
WHERE {InboxMessages.MessageId} = @{InboxMessages.MessageId}
    AND {InboxMessages.IsHandled} = 0
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
