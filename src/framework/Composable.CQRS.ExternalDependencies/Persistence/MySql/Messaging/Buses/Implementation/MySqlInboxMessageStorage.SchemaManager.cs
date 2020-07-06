using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.MySql.SystemExtensions;
using T =  Composable.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.InboxMessageDatabaseSchemaStrings;

namespace Composable.Persistence.MySql.Messaging.Buses.Implementation
{
    partial class MySqlInboxPersistenceLayer
    {
        static class SchemaManager
        {
            public static async Task EnsureTablesExistAsync(IMySqlConnectionProvider connectionFactory)
            {
                await  connectionFactory.ExecuteNonQueryAsync($@"


    CREATE TABLE IF NOT EXISTS {T.TableName}
    (
	    {T.Identity} bigint NOT NULL AUTO_INCREMENT,
        {T.TypeId} uniqueidentifier NOT NULL,
        {T.MessageId} uniqueidentifier NOT NULL,
	    {T.Status} smallintNOT NULL,
	    {T.Body} nvarchar(MAX) NOT NULL,
        {T.ExceptionCount} int NOT NULL DEFAULT 0,
        {T.ExceptionType} nvarchar(500) NULL,
        {T.ExceptionStackTrace} nvarchar(MAX) NULL,
        {T.ExceptionMessage} nvarchar(MAX) NULL,


        PRIMARY KEY ( {T.Identity} ),

        UNIQUE INDEX IX_{T.TableName}_Unique_{T.MessageId} ( {T.MessageId} )

    )

");
            }
        }
    }
}
