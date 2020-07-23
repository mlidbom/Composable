using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.Oracle.SystemExtensions;
using Composable.SystemCE.Reflection.Threading;
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
                await  connectionFactory.UseCommandAsync(
                    command => command.SetCommandText($@"

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
                {Message.Body}                  NCLOB                                   NOT NULL,
                {Message.ExceptionCount}        int DEFAULT 0                           NOT NULL,
                {Message.ExceptionType}         varchar(500)                            NULL,
                {Message.ExceptionStackTrace}   NCLOB                                   NULL,
                {Message.ExceptionMessage}      NCLOB                                   NULL,


                CONSTRAINT {Message.TableName}_PK PRIMARY KEY ({Message.GeneratedId}),

                CONSTRAINT {Message.TableName}_Unique_{Message.MessageId} UNIQUE ( {Message.MessageId} )
            )';

    end if;
end;
")
                                      .ExecuteNonQueryAsync()).NoMarshalling();
            }
        }
    }
}
