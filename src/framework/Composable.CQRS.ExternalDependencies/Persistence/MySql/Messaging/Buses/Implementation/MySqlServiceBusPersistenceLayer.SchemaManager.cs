using System.Threading.Tasks;
using Composable.Persistence.MySql.SystemExtensions;
using MessageTable = Composable.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.OutboxMessagesDatabaseSchemaStrings;
using DispatchingTable = Composable.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.OutboxMessageDispatchingTableSchemaStrings;

namespace Composable.Persistence.MySql.Messaging.Buses.Implementation
{
    partial class MySqlOutboxPersistenceLayer
    {
        static class SchemaManager
        {
            public static async Task EnsureTablesExistAsync(IMySqlConnectionProvider connectionFactory)
            {
                await connectionFactory.ExecuteNonQueryAsync($@"
IF NOT EXISTS (select name from sys.tables where name = '{MessageTable.TableName}')
BEGIN
    CREATE TABLE [dbo].[{MessageTable.TableName}]
    (
	    [{MessageTable.Identity}] [bigint] IDENTITY(1,1) NOT NULL,
        [{MessageTable.TypeIdGuidValue}] [uniqueidentifier] NOT NULL,
        [{MessageTable.MessageId}] [uniqueidentifier] NOT NULL,
	    [{MessageTable.SerializedMessage}] [nvarchar](MAX) NOT NULL,

        CONSTRAINT [PK_{MessageTable.TableName}] PRIMARY KEY CLUSTERED 
        (
	        [{MessageTable.Identity}] ASC
        ),

        CONSTRAINT IX_{MessageTable.TableName}_Unique_{MessageTable.MessageId} UNIQUE
        (
            {MessageTable.MessageId}
        )

    ) ON [PRIMARY]

    CREATE TABLE [dbo].[{DispatchingTable.TableName}]
    (
	    [{DispatchingTable.MessageId}] [uniqueidentifier] NOT NULL,
        [{DispatchingTable.EndpointId}] [uniqueidentifier] NOT NULL,
        [{DispatchingTable.IsReceived}] [bit] NOT NULL,

        CONSTRAINT [PK_{DispatchingTable.TableName}] 
            PRIMARY KEY CLUSTERED( {DispatchingTable.MessageId}, {DispatchingTable.EndpointId})
            WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF) ON [PRIMARY],

        CONSTRAINT FK_{DispatchingTable.TableName}_{DispatchingTable.MessageId} 
            FOREIGN KEY ( [{DispatchingTable.MessageId}] )  
            REFERENCES {MessageTable.TableName} ([{MessageTable.MessageId}])

    ) ON [PRIMARY]
END
");
            }
        }
    }
}
