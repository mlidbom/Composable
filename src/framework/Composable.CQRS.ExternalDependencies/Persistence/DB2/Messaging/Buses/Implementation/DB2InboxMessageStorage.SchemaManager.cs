using System.Threading.Tasks;
using Composable.Persistence.Common.AdoCE;
using Composable.Persistence.DB2.SystemExtensions;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.TasksCE;
using Message =  Composable.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.InboxMessageDatabaseSchemaStrings;

namespace Composable.Persistence.DB2.Messaging.Buses.Implementation
{
    partial class DB2InboxPersistenceLayer
    {
        const string DB2GuidType = "CHAR(36)";
        static class SchemaManager
        {
            public static async Task EnsureTablesExistAsync(IDB2ConnectionPool connectionFactory)
            {
                await  connectionFactory.UseCommandAsync(
                    command => command.SetCommandText($@"

begin
    declare continue handler for sqlstate '42710' begin end; --Ignore error if table exists
      
    EXECUTE IMMEDIATE '

        CREATE TABLE {Message.TableName}
        (
            {Message.GeneratedId}           BIGINT GENERATED ALWAYS AS IDENTITY  NOT NULL,
            {Message.TypeId}                {DB2GuidType}                        NOT NULL,
            {Message.MessageId}             {DB2GuidType}                        NOT NULL,
            {Message.Status}                smallint                             NOT NULL,
            {Message.Body}                  CLOB                                 NOT NULL,
            {Message.ExceptionCount}        integer DEFAULT 0                    NOT NULL,
            {Message.ExceptionType}         varchar(500)                         NULL,
            {Message.ExceptionStackTrace}   CLOB                                 NULL,
            {Message.ExceptionMessage}      CLOB                                 NULL,


            PRIMARY KEY ({Message.GeneratedId}),

            CONSTRAINT {Message.TableName}_Unique_{Message.MessageId} UNIQUE ( {Message.MessageId} )
        );
    ';

end;
")
                                      .ExecuteNonQueryAsync()).NoMarshalling();
            }
        }
    }
}
