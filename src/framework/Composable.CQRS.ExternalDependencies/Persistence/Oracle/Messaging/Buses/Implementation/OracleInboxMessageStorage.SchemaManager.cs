using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.Oracle.SystemExtensions;
using Message =  Composable.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.InboxMessageDatabaseSchemaStrings;

namespace Composable.Persistence.Oracle.Messaging.Buses.Implementation
{
    partial class OracleInboxPersistenceLayer
    {
        const string OracleGuidType = "CHAR(36)";
        static class SchemaManager
        {
            public static async Task EnsureTablesExistAsync(IOracleConnectionProvider connectionFactory)
            {
                await  connectionFactory.ExecuteNonQueryAsync($@"

declare existing_table_count integer;
begin
    select count(*) into existing_table_count from user_tables where table_name='{Message.TableName}';
    if (existing_table_count = 0) then
        EXECUTE IMMEDIATE '
            CREATE TABLE {Message.TableName}
            (
                {Message.GeneratedId}           NUMBER(19) GENERATED ALWAYS AS IDENTITY NOT NULL,
                {Message.TypeId}                {OracleGuidType}                        NOT NULL,
                {Message.MessageId}             {OracleGuidType}                        NOT NULL,
                {Message.Status}                smallint                                NOT NULL,
                {Message.Body}                  mediumtext                              NOT NULL,
                {Message.ExceptionCount}        int                                     NOT NULL  DEFAULT 0,
                {Message.ExceptionType}         varchar(500)                            NULL,
                {Message.ExceptionStackTrace}   mediumtext                              NULL,
                {Message.ExceptionMessage}      mediumtext                              NULL,


                CONSTRAINT {Message.TableName}_PK PRIMARY KEY ({Message.GeneratedId}),

                CONSTRAINT {Message.TableName}_Unique_{Message.MessageId} UNIQUE ( {Message.MessageId} )
            )';

");
            }
        }
    }
}
