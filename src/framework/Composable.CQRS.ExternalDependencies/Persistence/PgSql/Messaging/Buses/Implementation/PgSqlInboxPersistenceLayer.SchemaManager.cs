using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.PgSql.SystemExtensions;
using Composable.SystemCE.Reflection.Threading;
using Message =  Composable.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.InboxMessageDatabaseSchemaStrings;

namespace Composable.Persistence.PgSql.Messaging.Buses.Implementation
{
    partial class PgSqlInboxPersistenceLayer
    {
        const string PgSqlGuidType = "CHAR(36)";
        static class SchemaManager
        {
            public static async Task EnsureTablesExistAsync(INpgsqlConnectionProvider connectionFactory)
            {

                await connectionFactory.ExecuteNonQueryAsync($@"


    CREATE TABLE IF NOT EXISTS {Message.TableName}
    (
        {Message.GeneratedId}           bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
        {Message.TypeId}                {PgSqlGuidType}                     NOT NULL,
        {Message.MessageId}             {PgSqlGuidType}                     NOT NULL,
        {Message.Status}                smallint                            NOT NULL,
        {Message.Body}                  text                                NOT NULL,
        {Message.ExceptionCount}        int                                 NOT NULL  DEFAULT 0,
        {Message.ExceptionType}         varchar(500)                        NULL,
        {Message.ExceptionStackTrace}   text                                NULL,
        {Message.ExceptionMessage}      text                                NULL,


        PRIMARY KEY ( {Message.GeneratedId} ),

        CONSTRAINT IX_{Message.TableName}_Unique_{Message.MessageId} UNIQUE ( {Message.MessageId} )
    );



").NoMarshalling();

            }
        }
    }
}
