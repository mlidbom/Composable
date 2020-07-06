using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.MsSql.SystemExtensions;
using T =  Composable.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.InboxMessageDatabaseSchemaStrings;

namespace Composable.Persistence.MsSql.Messaging.Buses.Implementation
{
    partial class MsSqlInboxPersistenceLayer
    {
        static class SchemaManager
        {
            public static async Task EnsureTablesExistAsync(IMsSqlConnectionProvider connectionFactory)
            {
                await  connectionFactory.ExecuteNonQueryAsync($@"
IF NOT EXISTS(select name from sys.tables where name = '{T.TableName}')
BEGIN
    CREATE TABLE {T.TableName}
    (
	    [{T.Identity}] bigint IDENTITY(1,1) NOT NULL,
        {T.TypeId} uniqueidentifier NOT NULL,
        {T.MessageId} uniqueidentifier NOT NULL,
	    {T.Status} smallint NOT NULL,
	    {T.Body} nvarchar(MAX) NOT NULL,
        {T.ExceptionCount} int NOT NULL DEFAULT 0,
        {T.ExceptionType} nvarchar(500) NULL,
        {T.ExceptionStackTrace} nvarchar(MAX) NULL,
        {T.ExceptionMessage} nvarchar(MAX) NULL,


        CONSTRAINT PK_{T.TableName} PRIMARY KEY CLUSTERED ( [{T.Identity}] ASC ),

        CONSTRAINT IX_{T.TableName}_Unique_{T.MessageId} UNIQUE ( {T.MessageId} )

    )
END
");
            }
        }
    }
}
