﻿using System;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.PgSql.SystemExtensions;
using Composable.System.Threading;
using Schema =  Composable.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.InboxMessageDatabaseSchemaStrings;

namespace Composable.Persistence.PgSql.Messaging.Buses.Implementation
{
    partial class PgSqlInboxPersistenceLayer : IServiceBusPersistenceLayer.IInboxPersistenceLayer
    {
        readonly INpgsqlConnectionProvider _connectionFactory;
        public PgSqlInboxPersistenceLayer(INpgsqlConnectionProvider connectionFactory) => _connectionFactory = connectionFactory;

        public void SaveMessage(Guid messageId, Guid typeId, string serializedMessage)
        {
            _connectionFactory.UseCommand(
                command =>
                {
                    command
                       .SetCommandText(
                            $@"
INSERT INTO {Schema.TableName} 
            ({Schema.MessageId},  {Schema.TypeId},  {Schema.Body}, {Schema.Status}) 
    VALUES (@{Schema.MessageId}, @{Schema.TypeId}, @{Schema.Body}, {(int)Inbox.MessageStatus.UnHandled});
")
                       .AddParameter(Schema.MessageId, messageId)
                       .AddParameter(Schema.TypeId, typeId)
                        //performance: Like with the event store, keep all framework properties out of the JSON and put it into separate columns instead. For events. Reuse a pre-serialized instance from the persisting to the event store.
                       .AddMediumTextParameter(Schema.Body, serializedMessage)
                       .ExecuteNonQuery();
                });
        }

        public void MarkAsSucceeded(Guid messageId)
        {
            _connectionFactory.UseCommand(
                command =>
                {
                    var affectedRows = command
                                      .SetCommandText(
                                           $@"
UPDATE {Schema.TableName} 
    SET {Schema.Status} = {(int)Inbox.MessageStatus.Succeeded}
WHERE {Schema.MessageId} = @{Schema.MessageId}
    AND {Schema.Status} = {(int)Inbox.MessageStatus.UnHandled};
")
                                      .AddParameter(Schema.MessageId, messageId)
                                      .ExecuteNonQuery();

                    Assert.Result.Assert(affectedRows == 1);
                    return affectedRows;
                });
        }

        public int RecordException(Guid messageId, string exceptionStackTrace, string exceptionMessage, string exceptionType)
        {
            return _connectionFactory.UseCommand(
                command => command
                          .SetCommandText(
                               $@"
UPDATE {Schema.TableName} 
    SET {Schema.ExceptionCount} = {Schema.ExceptionCount} + 1,
        {Schema.ExceptionType} = @{Schema.ExceptionType},
        {Schema.ExceptionStackTrace} = @{Schema.ExceptionStackTrace},
        {Schema.ExceptionMessage} = @{Schema.ExceptionMessage}
        
WHERE {Schema.MessageId} = @{Schema.MessageId};
")
                          .AddParameter(Schema.MessageId, messageId)
                          .AddMediumTextParameter(Schema.ExceptionStackTrace, exceptionStackTrace)
                          .AddMediumTextParameter(Schema.ExceptionMessage, exceptionMessage)
                          .AddVarcharParameter(Schema.ExceptionType, 500, exceptionType)
                          .ExecuteNonQuery());
        }

        public int MarkAsFailed(Guid messageId)
        {
            return _connectionFactory.UseCommand(
                command => command
                          .SetCommandText(
                               $@"
UPDATE {Schema.TableName} 
    SET {Schema.Status} = {(int)Inbox.MessageStatus.Failed}
WHERE {Schema.MessageId} = @{Schema.MessageId}
    AND {Schema.Status} = {(int)Inbox.MessageStatus.UnHandled};")
                          .AddParameter(Schema.MessageId, messageId)
                          .ExecuteNonQuery());
        }

        public async Task InitAsync() => await SchemaManager.EnsureTablesExistAsync(_connectionFactory).NoMarshalling();
    }
}