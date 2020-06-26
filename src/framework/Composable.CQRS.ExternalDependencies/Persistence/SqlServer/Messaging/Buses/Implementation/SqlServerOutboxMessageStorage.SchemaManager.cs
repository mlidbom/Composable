using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.SqlServer.SystemExtensions;

namespace Composable.Persistence.SqlServer.Messaging.Buses.Implementation
{
    partial class SqlServerOutboxMessageStorage
    {
        static class SchemaManager
        {
            public static async Task EnsureTablesExistAsync(ISqlServerConnectionProvider connectionFactory)
            {
                await using var connection = connectionFactory.OpenConnection();
                await connection.ExecuteNonQueryAsync($@"
IF NOT EXISTS (select name from sys.tables where name = '{OutboxMessagesDatabaseSchemaStrings.TableName}')
BEGIN
    CREATE TABLE [dbo].[{OutboxMessagesDatabaseSchemaStrings.TableName}]
    (
	    [{OutboxMessagesDatabaseSchemaStrings.Identity}] [int] IDENTITY(1,1) NOT NULL,
        [{OutboxMessagesDatabaseSchemaStrings.TypeIdGuidValue}] [uniqueidentifier] NOT NULL,
        [{OutboxMessagesDatabaseSchemaStrings.MessageId}] [uniqueidentifier] NOT NULL,
	    [{OutboxMessagesDatabaseSchemaStrings.Body}] [nvarchar](MAX) NOT NULL,

        CONSTRAINT [PK_{OutboxMessagesDatabaseSchemaStrings.TableName}] PRIMARY KEY CLUSTERED 
        (
	        [{OutboxMessagesDatabaseSchemaStrings.Identity}] ASC
        ),

        CONSTRAINT IX_{OutboxMessagesDatabaseSchemaStrings.TableName}_Unique_{OutboxMessagesDatabaseSchemaStrings.MessageId} UNIQUE
        (
            {OutboxMessagesDatabaseSchemaStrings.MessageId}
        )

    ) ON [PRIMARY]

    CREATE TABLE [dbo].[{OutboxMessageDispatchingTableSchemaStrings.TableName}]
    (
	    [{OutboxMessageDispatchingTableSchemaStrings.MessageId}] [uniqueidentifier] NOT NULL,
        [{OutboxMessageDispatchingTableSchemaStrings.EndpointId}] [uniqueidentifier] NOT NULL,
        [{OutboxMessageDispatchingTableSchemaStrings.IsReceived}] [bit] NOT NULL,

        CONSTRAINT [PK_{OutboxMessageDispatchingTableSchemaStrings.TableName}] 
            PRIMARY KEY CLUSTERED( {OutboxMessageDispatchingTableSchemaStrings.MessageId}, {OutboxMessageDispatchingTableSchemaStrings.EndpointId})
            WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF) ON [PRIMARY],

        CONSTRAINT FK_{OutboxMessageDispatchingTableSchemaStrings.TableName}_{OutboxMessageDispatchingTableSchemaStrings.MessageId} 
            FOREIGN KEY ( [{OutboxMessageDispatchingTableSchemaStrings.MessageId}] )  
            REFERENCES {OutboxMessagesDatabaseSchemaStrings.TableName} ([{OutboxMessagesDatabaseSchemaStrings.MessageId}])

    ) ON [PRIMARY]
END
");
            }
        }
    }
}
