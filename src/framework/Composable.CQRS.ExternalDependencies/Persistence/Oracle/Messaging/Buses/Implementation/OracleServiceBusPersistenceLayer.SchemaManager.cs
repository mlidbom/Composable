using System.Threading.Tasks;
using Composable.Persistence.Oracle.SystemExtensions;
using Message = Composable.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.OutboxMessagesDatabaseSchemaStrings;
using Dispatch = Composable.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.OutboxMessageDispatchingTableSchemaStrings;

namespace Composable.Persistence.Oracle.Messaging.Buses.Implementation
{
    partial class OracleOutboxPersistenceLayer
    {
        const string OracleGuidType = "CHAR(36)";
        static class SchemaManager
        {
            public static async Task EnsureTablesExistAsync(IOracleConnectionProvider connectionFactory)
            {
                //Urgent: Figure out the syntax for the commented out parts.
                await connectionFactory.ExecuteNonQueryAsync($@"
declare existing_table_count integer;
begin
    select count(*) into existing_table_count from user_tables where table_name='{Message.TableName}';
    if (existing_table_count = 0) then

        EXECUTE IMMEDIATE '
            CREATE TABLE {Message.TableName}
                (
	                {Message.GeneratedId} NUMBER(19) GENERATED ALWAYS AS IDENTITY NOT NULL,
                    {Message.TypeIdGuidValue} {OracleGuidType} NOT NULL,
                    {Message.MessageId} {OracleGuidType} NOT NULL,
	                {Message.SerializedMessage} NCLOB NOT NULL,

                    CONSTRAINT {Message.TableName}_PK PRIMARY KEY ({Message.GeneratedId}),

                    CONSTRAINT {Message.TableName}_Unique_{Message.MessageId} UNIQUE ( {Message.MessageId} )

                )';

        EXECUTE IMMEDIATE '        
            CREATE TABLE  IF NOT EXISTS {Dispatch.TableName}
            (
	            {Dispatch.MessageId} {OracleGuidType} NOT NULL,
                {Dispatch.EndpointId} {OracleGuidType} NOT NULL,
                {Dispatch.IsReceived} bit NOT NULL,

               CONSTRAINT {Message.TableName}_PK PRIMARY KEY ({Message.MessageId}, {Dispatch.EndpointId})
                    /*WITH (ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF) ON PRIMARY,*/
            )';

        EXECUTE IMMEDIATE '
            ALTER TABLE {Dispatch.TableName} ADD CONSTRAINT FK_{Dispatch.MessageId} 
                FOREIGN KEY ( {Dispatch.MessageId} ) REFERENCES {Message.TableName} ({Message.MessageId})';

    end if;
end;
");
            }
        }
    }
}
