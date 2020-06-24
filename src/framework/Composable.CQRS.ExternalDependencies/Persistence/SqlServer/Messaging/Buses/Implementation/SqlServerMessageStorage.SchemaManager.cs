using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.SqlServer.SystemExtensions;

namespace Composable.Persistence.SqlServer.Messaging.Buses.Implementation
{
    partial class SqlServerMessageStorage
    {
        static class SchemaManager
        {
            public static async Task EnsureTablesExistAsync(ISqlConnectionProvider connectionFactory)
            {
                await using var connection = connectionFactory.OpenConnection();
                await connection.ExecuteNonQueryAsync($@"
IF NOT EXISTS(select name from sys.tables where name = '{InboxMessageDatabaseSchemaStrings.TableName}')
BEGIN
    CREATE TABLE [dbo].[{InboxMessageDatabaseSchemaStrings.TableName}]
    (
	    [{InboxMessageDatabaseSchemaStrings.Identity}] [int] IDENTITY(1,1) NOT NULL,
        [{InboxMessageDatabaseSchemaStrings.TypeId}] [uniqueidentifier] NOT NULL,
        [{InboxMessageDatabaseSchemaStrings.MessageId}] [uniqueidentifier] NOT NULL,
	    [{InboxMessageDatabaseSchemaStrings.Status}] [smallint]NOT NULL,
	    [{InboxMessageDatabaseSchemaStrings.Body}] [nvarchar](MAX) NOT NULL,
        [{InboxMessageDatabaseSchemaStrings.ExceptionCount}] [int] NOT NULL DEFAULT 0,
        [{InboxMessageDatabaseSchemaStrings.ExceptionType}] [nvarchar](500) NULL,
        [{InboxMessageDatabaseSchemaStrings.ExceptionStackTrace}] [nvarchar](MAX) NULL,
        [{InboxMessageDatabaseSchemaStrings.ExceptionMessage}] [nvarchar](MAX) NULL,


        CONSTRAINT [PK_{InboxMessageDatabaseSchemaStrings.TableName}] PRIMARY KEY CLUSTERED 
        (
	        [{InboxMessageDatabaseSchemaStrings.Identity}] ASC
        ),

        CONSTRAINT IX_{InboxMessageDatabaseSchemaStrings.TableName}_Unique_{InboxMessageDatabaseSchemaStrings.MessageId} UNIQUE
        (
            {InboxMessageDatabaseSchemaStrings.MessageId}
        )

    ) ON [PRIMARY]
END
");
            }
        }
    }
}
