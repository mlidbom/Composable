using System.Threading.Tasks;
using Composable.Persistence.PgSql.SystemExtensions;
using M = Composable.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.OutboxMessagesDatabaseSchemaStrings;
using D = Composable.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.OutboxMessageDispatchingTableSchemaStrings;

namespace Composable.Persistence.PgSql.Messaging.Buses.Implementation
{
    partial class PgSqlOutboxPersistenceLayer
    {
        const string PgSqlGuidType = "CHAR(36)";
        static class SchemaManager
        {
            public static async Task EnsureTablesExistAsync(INpgsqlConnectionProvider connectionFactory)
            {
                //Urgent: Figure out the syntax for the commented out parts.
                await connectionFactory.ExecuteNonQueryAsync($@"
    CREATE TABLE IF NOT EXISTS {M.TableName}
    (
	    {M.GeneratedId} bigint NOT NULL AUTO_INCREMENT,
        {M.TypeIdGuidValue} {PgSqlGuidType} NOT NULL,
        {M.MessageId} {PgSqlGuidType} NOT NULL,
	    {M.SerializedMessage} MEDIUMTEXT NOT NULL,

        PRIMARY KEY ( {M.GeneratedId}),

        UNIQUE INDEX IX_{M.TableName}_Unique_{M.MessageId} ( {M.MessageId} )

    )



    CREATE TABLE  IF NOT EXISTS {D.TableName}
    (
	    {D.MessageId} {PgSqlGuidType} NOT NULL,
        {D.EndpointId} {PgSqlGuidType} NOT NULL,
        {D.IsReceived} bit NOT NULL,

       
        PRIMARY KEY ( {D.MessageId}, {D.EndpointId}),
            /*WITH (ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF) ON PRIMARY,*/

        FOREIGN KEY ({D.MessageId}) REFERENCES {M.TableName} ({M.MessageId})

    )



");
            }
        }
    }
}
