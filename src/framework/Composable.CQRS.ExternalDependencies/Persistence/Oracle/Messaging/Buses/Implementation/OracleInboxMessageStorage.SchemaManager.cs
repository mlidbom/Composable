using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.Oracle.SystemExtensions;
using T =  Composable.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.InboxMessageDatabaseSchemaStrings;

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


    CREATE TABLE IF NOT EXISTS {T.TableName}
    (
	    {T.Identity} bigint NOT NULL AUTO_INCREMENT,
        {T.TypeId} {OracleGuidType} NOT NULL,
        {T.MessageId} {OracleGuidType} NOT NULL,
	    {T.Status} smallint NOT NULL,
	    {T.Body} mediumtext NOT NULL,
        {T.ExceptionCount} int NOT NULL DEFAULT 0,
        {T.ExceptionType} varchar(500) NULL,
        {T.ExceptionStackTrace} mediumtext NULL,
        {T.ExceptionMessage} mediumtext NULL,


        PRIMARY KEY ( {T.Identity} ),

        UNIQUE INDEX IX_{T.TableName}_Unique_{T.MessageId} ( {T.MessageId} )
    )
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb4;

");
            }
        }
    }
}
