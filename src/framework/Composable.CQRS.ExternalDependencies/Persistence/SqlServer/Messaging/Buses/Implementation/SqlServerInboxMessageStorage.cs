using System;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.SqlServer.SystemExtensions;
using Composable.System.Reflection;
using Schema =  Composable.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.InboxMessageDatabaseSchemaStrings;

namespace Composable.Persistence.SqlServer.Messaging.Buses.Implementation
{
    partial class SqlServerInboxMessageStorage : Inbox.IMessageStorage
    {
        readonly ISqlServerConnectionProvider _connectionFactory;

        public SqlServerInboxMessageStorage(ISqlServerConnectionProvider connectionFactory) => _connectionFactory = connectionFactory;

        public void SaveIncomingMessage(TransportMessage.InComing message) =>
            _connectionFactory.UseCommand(
                command =>
                {
                    command
                       .SetCommandText(
                            $@"
INSERT {Schema.TableName} 
            ({Schema.MessageId},  {Schema.TypeId},  {Schema.Body}, {Schema.Status}) 
    VALUES (@{Schema.MessageId}, @{Schema.TypeId}, @{Schema.Body}, {(int)Inbox.MessageStatus.UnHandled})
")
                       .AddParameter(Schema.MessageId, message.MessageId)
                       .AddParameter(Schema.TypeId, message.MessageTypeId.GuidValue)
                        //performance: Like with the event store, keep all framework properties out of the JSON and put it into separate columns instead. For events. Reuse a pre-serialized instance from the persisting to the event store.
                       .AddNVarcharMaxParameter(Schema.Body, message.Body)
                       .ExecuteNonQuery();
                });

        public void MarkAsSucceeded(TransportMessage.InComing message) =>
            _connectionFactory.UseCommand(
                command =>
                {
                    var affectedRows = command
                                      .SetCommandText(
                                           $@"
UPDATE {Schema.TableName} 
    SET {Schema.Status} = {(int)Inbox.MessageStatus.Succeeded}
WHERE {Schema.MessageId} = @{Schema.MessageId}
    AND {Schema.Status} = {(int)Inbox.MessageStatus.UnHandled}
")
                                      .AddParameter(Schema.MessageId, message.MessageId)
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
UPDATE {Schema.TableName} 
    SET {Schema.ExceptionCount} = {Schema.ExceptionCount} + 1,
        {Schema.ExceptionType} = @{Schema.ExceptionType},
        {Schema.ExceptionStackTrace} = @{Schema.ExceptionStackTrace},
        {Schema.ExceptionMessage} = @{Schema.ExceptionMessage}
        
WHERE {Schema.MessageId} = @{Schema.MessageId}
")
                                      .AddParameter(Schema.MessageId, message.MessageId)
                                      .AddNVarcharMaxParameter(Schema.ExceptionStackTrace, exception.StackTrace)
                                      .AddNVarcharMaxParameter(Schema.ExceptionMessage, exception.Message)
                                      .AddNVarcharParameter(Schema.ExceptionType, 500, exception.GetType().GetFullNameCompilable())
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
UPDATE {Schema.TableName} 
    SET {Schema.Status} = {(int)Inbox.MessageStatus.Failed}
WHERE {Schema.MessageId} = @{Schema.MessageId}
    AND {Schema.Status} = {(int)Inbox.MessageStatus.UnHandled}
")
                                      .AddParameter(Schema.MessageId, message.MessageId)
                                      .ExecuteNonQuery();

                    Assert.Result.Assert(affectedRows == 1);
                    return affectedRows;
                });

        public async Task StartAsync() => await SchemaManager.EnsureTablesExistAsync(_connectionFactory);
    }
}
