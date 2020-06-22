using System;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.System.Data.SqlClient;
using Composable.System.Reflection;

namespace Composable.Messaging.Buses.Implementation
{
    partial class SqlServerMessageStorage : Inbox.IMessageStorage
    {
        readonly ISqlConnectionProvider _connectionFactory;

        public SqlServerMessageStorage(ISqlConnectionProvider connectionFactory) => _connectionFactory = connectionFactory;

        public void SaveIncomingMessage(TransportMessage.InComing message) =>
            _connectionFactory.UseCommand(
                command =>
                {
                    command
                       .SetCommandText(
                            $@"
INSERT {InboxMessageDatabaseSchemaStrings.TableName} 
            ({InboxMessageDatabaseSchemaStrings.MessageId},  {InboxMessageDatabaseSchemaStrings.TypeId},  {InboxMessageDatabaseSchemaStrings.Body}, {InboxMessageDatabaseSchemaStrings.Status}) 
    VALUES (@{InboxMessageDatabaseSchemaStrings.MessageId}, @{InboxMessageDatabaseSchemaStrings.TypeId}, @{InboxMessageDatabaseSchemaStrings.Body}, {(int)Inbox.MessageStatus.UnHandled})
")
                       .AddParameter(InboxMessageDatabaseSchemaStrings.MessageId, message.MessageId)
                       .AddParameter(InboxMessageDatabaseSchemaStrings.TypeId, message.MessageTypeId.GuidValue)
                        //performance: Like with the event store, keep all framework properties out of the JSON and put it into separate columns instead. For events. Reuse a pre-serialized instance from the persisting to the event store.
                       .AddNVarcharMaxParameter(InboxMessageDatabaseSchemaStrings.Body, message.Body)
                       .ExecuteNonQuery();
                });

        public void MarkAsSucceeded(TransportMessage.InComing message) =>
            _connectionFactory.UseCommand(
                command =>
                {
                    var affectedRows = command
                                      .SetCommandText(
                                           $@"
UPDATE {InboxMessageDatabaseSchemaStrings.TableName} 
    SET {InboxMessageDatabaseSchemaStrings.Status} = {(int)Inbox.MessageStatus.Succeeded}
WHERE {InboxMessageDatabaseSchemaStrings.MessageId} = @{InboxMessageDatabaseSchemaStrings.MessageId}
    AND {InboxMessageDatabaseSchemaStrings.Status} = {(int)Inbox.MessageStatus.UnHandled}
")
                                      .AddParameter(InboxMessageDatabaseSchemaStrings.MessageId, message.MessageId)
                                      .ExecuteNonQuery();

                    Assert.Result.Assert(affectedRows == 1);
                    return affectedRows;
                });

        public void RecordException(TransportMessage.InComing message, Exception exception) =>
            _connectionFactory.UseCommand(
                command =>
                {
                    var affectedRows = command
                                      .SetCommandText(
                                           $@"
UPDATE {InboxMessageDatabaseSchemaStrings.TableName} 
    SET {InboxMessageDatabaseSchemaStrings.ExceptionCount} = {InboxMessageDatabaseSchemaStrings.ExceptionCount} + 1,
        {InboxMessageDatabaseSchemaStrings.ExceptionType} = @{InboxMessageDatabaseSchemaStrings.ExceptionType},
        {InboxMessageDatabaseSchemaStrings.ExceptionStackTrace} = @{InboxMessageDatabaseSchemaStrings.ExceptionStackTrace},
        {InboxMessageDatabaseSchemaStrings.ExceptionMessage} = @{InboxMessageDatabaseSchemaStrings.ExceptionMessage}
        
WHERE {InboxMessageDatabaseSchemaStrings.MessageId} = @{InboxMessageDatabaseSchemaStrings.MessageId}
")
                                      .AddParameter(InboxMessageDatabaseSchemaStrings.MessageId, message.MessageId)
                                      .AddNVarcharMaxParameter(InboxMessageDatabaseSchemaStrings.ExceptionStackTrace, exception.StackTrace)
                                      .AddNVarcharMaxParameter(InboxMessageDatabaseSchemaStrings.ExceptionMessage, exception.Message)
                                      .AddNVarcharParameter(InboxMessageDatabaseSchemaStrings.ExceptionType, 500, exception.GetType().GetFullNameCompilable())
                                      .ExecuteNonQuery();

                    Assert.Result.Assert(affectedRows == 1);
                    return affectedRows;
                });

        public void MarkAsFailed(TransportMessage.InComing message) =>
            _connectionFactory.UseCommand(
                command =>
                {
                    var affectedRows = command
                                      .SetCommandText(
                                           $@"
UPDATE {InboxMessageDatabaseSchemaStrings.TableName} 
    SET {InboxMessageDatabaseSchemaStrings.Status} = {(int)Inbox.MessageStatus.Failed}
WHERE {InboxMessageDatabaseSchemaStrings.MessageId} = @{InboxMessageDatabaseSchemaStrings.MessageId}
    AND {InboxMessageDatabaseSchemaStrings.Status} = {(int)Inbox.MessageStatus.UnHandled}
")
                                      .AddParameter(InboxMessageDatabaseSchemaStrings.MessageId, message.MessageId)
                                      .ExecuteNonQuery();

                    Assert.Result.Assert(affectedRows == 1);
                    return affectedRows;
                });

        public async Task StartAsync() => await SchemaManager.EnsureTablesExistAsync(_connectionFactory);
    }
}
