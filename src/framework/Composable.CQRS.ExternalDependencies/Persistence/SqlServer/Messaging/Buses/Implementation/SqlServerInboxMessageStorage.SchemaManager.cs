using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.SqlServer.SystemExtensions;
using Schema =  Composable.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.InboxMessageDatabaseSchemaStrings;

namespace Composable.Persistence.SqlServer.Messaging.Buses.Implementation
{
    partial class SqlServerInboxPersistenceLayer
    {
        static class SchemaManager
        {
            public static async Task EnsureTablesExistAsync(ISqlServerConnectionProvider connectionFactory)
            {
                await  connectionFactory.ExecuteNonQueryAsync($@"
IF NOT EXISTS(select name from sys.tables where name = '{Schema.TableName}')
BEGIN
    CREATE TABLE [dbo].[{Schema.TableName}]
    (
	    [{Schema.Identity}] [bigint] IDENTITY(1,1) NOT NULL,
        [{Schema.TypeId}] [uniqueidentifier] NOT NULL,
        [{Schema.MessageId}] [uniqueidentifier] NOT NULL,
	    [{Schema.Status}] [smallint]NOT NULL,
	    [{Schema.Body}] [nvarchar](MAX) NOT NULL,
        [{Schema.ExceptionCount}] [int] NOT NULL DEFAULT 0,
        [{Schema.ExceptionType}] [nvarchar](500) NULL,
        [{Schema.ExceptionStackTrace}] [nvarchar](MAX) NULL,
        [{Schema.ExceptionMessage}] [nvarchar](MAX) NULL,


        CONSTRAINT [PK_{Schema.TableName}] PRIMARY KEY CLUSTERED 
        (
	        [{Schema.Identity}] ASC
        ),

        CONSTRAINT IX_{Schema.TableName}_Unique_{Schema.MessageId} UNIQUE
        (
            {Schema.MessageId}
        )

    ) ON [PRIMARY]
END
");
            }
        }
    }
}
