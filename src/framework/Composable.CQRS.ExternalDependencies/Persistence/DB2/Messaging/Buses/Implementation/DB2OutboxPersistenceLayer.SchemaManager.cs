using System.Threading.Tasks;
using Composable.Persistence.DB2.SystemExtensions;
using Composable.SystemCE.ThreadingCE;
using Message = Composable.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.OutboxMessagesDatabaseSchemaStrings;
using Dispatch = Composable.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.OutboxMessageDispatchingTableSchemaStrings;

namespace Composable.Persistence.DB2.Messaging.Buses.Implementation
{
    partial class DB2OutboxPersistenceLayer
    {
        const string DB2GuidType = "CHAR(36)";
        static class SchemaManager
        {
            public static async Task EnsureTablesExistAsync(IComposableDB2ConnectionProvider connectionFactory)
            {
                //Urgent: Figure out the syntax for the commented out parts.
                await connectionFactory.UseCommandAsync(
                    command => command.SetCommandText($@"
begin
    declare continue handler for sqlstate '42710' begin end; --Ignore error if table exists

        EXECUTE IMMEDIATE '
            CREATE TABLE {Message.TableName}
                (
                    {Message.GeneratedId}       BIGINT GENERATED ALWAYS AS IDENTITY NOT NULL,
                    {Message.TypeIdGuidValue}   {DB2GuidType}                       NOT NULL,
                    {Message.MessageId}         {DB2GuidType}                       NOT NULL,
                    {Message.SerializedMessage} CLOB                                NOT NULL,

                    PRIMARY KEY ({Message.GeneratedId}),

                    CONSTRAINT {Message.TableName}_Unique_{Message.MessageId} UNIQUE ( {Message.MessageId} )
                )';

        EXECUTE IMMEDIATE '        
            CREATE TABLE  {Dispatch.TableName}
            (
                {Dispatch.MessageId}  {DB2GuidType} NOT NULL,
                {Dispatch.EndpointId} {DB2GuidType} NOT NULL,
                {Dispatch.IsReceived} SMALLINT      NOT NULL,

                PRIMARY KEY ({Dispatch.MessageId}, {Dispatch.EndpointId})
                    /*WITH (ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF) ON PRIMARY,*/
            )';

        EXECUTE IMMEDIATE '
            ALTER TABLE {Dispatch.TableName} ADD CONSTRAINT FK_{Dispatch.MessageId} 
                FOREIGN KEY ( {Dispatch.MessageId} ) REFERENCES {Message.TableName} ({Message.MessageId})';

end;
")
                                      .ExecuteNonQueryAsync()).NoMarshalling();
            }
        }
    }
}
