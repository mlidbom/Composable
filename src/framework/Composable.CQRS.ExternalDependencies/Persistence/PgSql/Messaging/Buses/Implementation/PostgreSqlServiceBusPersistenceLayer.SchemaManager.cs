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


                await connectionFactory.ExecuteNonQueryAsync($@"
   

CREATE TABLE IF NOT EXISTS {M.TableName}
    (
	    {M.GeneratedId} bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
        {M.TypeIdGuidValue} {PgSqlGuidType} NOT NULL,
        {M.MessageId} {PgSqlGuidType} NOT NULL,
	    {M.SerializedMessage} TEXT NOT NULL,

        PRIMARY KEY ({M.GeneratedId}),

        CONSTRAINT IX_{M.TableName}_Unique_{M.MessageId} UNIQUE ( {M.MessageId} )

    );


    CREATE TABLE  IF NOT EXISTS {D.TableName}
    (
	    {D.MessageId} {PgSqlGuidType} NOT NULL,
        {D.EndpointId} {PgSqlGuidType} NOT NULL,
        {D.IsReceived} boolean NOT NULL,

       
        PRIMARY KEY ( {D.MessageId}, {D.EndpointId}),
            

        FOREIGN KEY ({D.MessageId}) REFERENCES {M.TableName} ({M.MessageId})

    );

");
            }
        }
    }
}
