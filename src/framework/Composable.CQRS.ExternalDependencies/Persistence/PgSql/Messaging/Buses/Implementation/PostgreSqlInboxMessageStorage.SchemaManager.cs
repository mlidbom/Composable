using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.PgSql.SystemExtensions;
using T =  Composable.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.InboxMessageDatabaseSchemaStrings;

namespace Composable.Persistence.PgSql.Messaging.Buses.Implementation
{
    partial class PgSqlInboxPersistenceLayer
    {
        const string PgSqlGuidType = "CHAR(36)";
        static class SchemaManager
        {
            public static async Task EnsureTablesExistAsync(INpgsqlConnectionProvider connectionFactory)
            {
                await  connectionFactory.ExecuteNonQueryAsync($@"


    CREATE TABLE IF NOT EXISTS {T.TableName}
    (
	    {T.Identity} bigint NOT NULL AUTO_INCREMENT,
        {T.TypeId} {PgSqlGuidType} NOT NULL,
        {T.MessageId} {PgSqlGuidType} NOT NULL,
	    {T.Status} smallint NOT NULL,
	    {T.Body} mediumtext NOT NULL,
        {T.ExceptionCount} int NOT NULL DEFAULT 0,
        {T.ExceptionType} varchar(500) NULL,
        {T.ExceptionStackTrace} mediumtext NULL,
        {T.ExceptionMessage} mediumtext NULL,


        PRIMARY KEY ( {T.Identity} ),

        UNIQUE INDEX IX_{T.TableName}_Unique_{T.MessageId} ( {T.MessageId} )
    )



");
            }
        }
    }
}
