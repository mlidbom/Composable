using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.SqlServer.SystemExtensions;

namespace Composable.Persistence.SqlServer.Messaging.Buses.Implementation
{
    partial class SqlServerInterProcessTransportMessageStorage
    {
        static class SchemaManager
        {
            public static async Task EnsureTablesExistAsync(ISqlConnectionProvider connectionFactory)
            {
                await using var connection = connectionFactory.OpenConnection();
                await connection.ExecuteNonQueryAsync($@"
IF NOT EXISTS (select name from sys.tables where name = '{InterprocessTransportOutboxMessagesDatabaseSchemaStrings.TableName}')
BEGIN
    CREATE TABLE [dbo].[{InterprocessTransportOutboxMessagesDatabaseSchemaStrings.TableName}]
    (
	    [{InterprocessTransportOutboxMessagesDatabaseSchemaStrings.Identity}] [int] IDENTITY(1,1) NOT NULL,
        [{InterprocessTransportOutboxMessagesDatabaseSchemaStrings.TypeIdGuidValue}] [uniqueidentifier] NOT NULL,
        [{InterprocessTransportOutboxMessagesDatabaseSchemaStrings.MessageId}] [uniqueidentifier] NOT NULL,
	    [{InterprocessTransportOutboxMessagesDatabaseSchemaStrings.Body}] [nvarchar](MAX) NOT NULL,

        CONSTRAINT [PK_{InterprocessTransportOutboxMessagesDatabaseSchemaStrings.TableName}] PRIMARY KEY CLUSTERED 
        (
	        [{InterprocessTransportOutboxMessagesDatabaseSchemaStrings.Identity}] ASC
        ),

        CONSTRAINT IX_{InterprocessTransportOutboxMessagesDatabaseSchemaStrings.TableName}_Unique_{InterprocessTransportOutboxMessagesDatabaseSchemaStrings.MessageId} UNIQUE
        (
            {InterprocessTransportOutboxMessagesDatabaseSchemaStrings.MessageId}
        )

    ) ON [PRIMARY]

    CREATE TABLE [dbo].[{InterprocessTransportMessageDispatchingDatabaseSchemaNames.TableName}]
    (
	    [{InterprocessTransportMessageDispatchingDatabaseSchemaNames.MessageId}] [uniqueidentifier] NOT NULL,
        [{InterprocessTransportMessageDispatchingDatabaseSchemaNames.EndpointId}] [uniqueidentifier] NOT NULL,
        [{InterprocessTransportMessageDispatchingDatabaseSchemaNames.IsReceived}] [bit] NOT NULL,

        CONSTRAINT [PK_{InterprocessTransportMessageDispatchingDatabaseSchemaNames.TableName}] 
            PRIMARY KEY CLUSTERED( {InterprocessTransportMessageDispatchingDatabaseSchemaNames.MessageId}, {InterprocessTransportMessageDispatchingDatabaseSchemaNames.EndpointId})
            WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF) ON [PRIMARY],

        CONSTRAINT FK_{InterprocessTransportMessageDispatchingDatabaseSchemaNames.TableName}_{InterprocessTransportMessageDispatchingDatabaseSchemaNames.MessageId} 
            FOREIGN KEY ( [{InterprocessTransportMessageDispatchingDatabaseSchemaNames.MessageId}] )  
            REFERENCES {InterprocessTransportOutboxMessagesDatabaseSchemaStrings.TableName} ([{InterprocessTransportOutboxMessagesDatabaseSchemaStrings.MessageId}])

    ) ON [PRIMARY]
END
");
            }
        }
    }
}
