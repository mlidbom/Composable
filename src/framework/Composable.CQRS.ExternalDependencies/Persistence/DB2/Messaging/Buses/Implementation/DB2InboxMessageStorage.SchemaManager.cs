using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.DB2.SystemExtensions;
using Composable.System.Threading;
using Message =  Composable.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.InboxMessageDatabaseSchemaStrings;

namespace Composable.Persistence.DB2.Messaging.Buses.Implementation
{
    partial class DB2InboxPersistenceLayer
    {
        const string DB2GuidType = "CHAR(36)";
        static class SchemaManager
        {
            public static async Task EnsureTablesExistAsync(IDB2ConnectionProvider connectionFactory)
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
                {Message.TypeId}                {DB2GuidType}                        NOT NULL,
                {Message.MessageId}             {DB2GuidType}                        NOT NULL,
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
