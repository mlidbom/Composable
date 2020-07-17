using System.Threading.Tasks;
using Composable.Persistence.DB2.SystemExtensions;
using Composable.System.Threading;
using Message = Composable.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.OutboxMessagesDatabaseSchemaStrings;
using Dispatch = Composable.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.OutboxMessageDispatchingTableSchemaStrings;

namespace Composable.Persistence.DB2.Messaging.Buses.Implementation
{
    partial class DB2OutboxPersistenceLayer
    {
        const string DB2GuidType = "CHAR(36)";
        static class SchemaManager
        {
            public static async Task EnsureTablesExistAsync(IDB2ConnectionProvider connectionFactory)
            {
                //Urgent: Figure out the syntax for the commented out parts.
                await connectionFactory.UseCommandAsync(
                    command => command.SetCommandText($@"
declare existing_table_count integer;
begin
    select count(*) into existing_table_count from user_tables where table_name='{Message.TableName}';
    if (existing_table_count = 0) then

        EXECUTE IMMEDIATE '
            CREATE TABLE {Message.TableName}
                (
                    {Message.GeneratedId}       NUMBER(19) GENERATED ALWAYS AS IDENTITY NOT NULL,
                    {Message.TypeIdGuidValue}   {DB2GuidType}                        NOT NULL,
                    {Message.MessageId}         {DB2GuidType}                        NOT NULL,
                    {Message.SerializedMessage} NCLOB                                   NOT NULL,

                    CONSTRAINT PK_{Message.TableName} PRIMARY KEY ({Message.GeneratedId}),

                    CONSTRAINT {Message.TableName}_Unique_{Message.MessageId} UNIQUE ( {Message.MessageId} )
                )';

        EXECUTE IMMEDIATE '        
            CREATE TABLE  {Dispatch.TableName}
            (
                {Dispatch.MessageId}  {DB2GuidType} NOT NULL,
                {Dispatch.EndpointId} {DB2GuidType} NOT NULL,
                {Dispatch.IsReceived} NUMBER(1)        NOT NULL,

                CONSTRAINT PK_{Dispatch.TableName} PRIMARY KEY ({Dispatch.MessageId}, {Dispatch.EndpointId})
                    /*WITH (ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF) ON PRIMARY,*/
            )';

        EXECUTE IMMEDIATE '
            ALTER TABLE {Dispatch.TableName} ADD CONSTRAINT FK_{Dispatch.MessageId} 
                FOREIGN KEY ( {Dispatch.MessageId} ) REFERENCES {Message.TableName} ({Message.MessageId})';

        end if;
    end;
")
                                      .ExecuteNonQueryAsync()).NoMarshalling();
            }
        }
    }
}
