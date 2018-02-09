using System.Threading.Tasks;
using Composable.System.Data.SqlClient;

namespace Composable.Messaging.Buses.Implementation
{
    partial class InterprocessTransport
    {
        static class OutboxMessages
        {
            internal const string TableName = nameof(OutboxMessages);

            internal const string Identity = nameof(Identity);
            internal const string TypeIdGuidValue = nameof(TypeIdGuidValue);
            internal const string MessageId = nameof(MessageId);
            internal const string Body = nameof(Body);
        }

        static class MessageDispatching
        {
            internal const string TableName = nameof(MessageDispatching);

            internal const string MessageId = nameof(MessageId);
            internal const string EndpointId = nameof(EndpointId);
            internal const string IsReceived = nameof(IsReceived);
        }

        partial class MessageStorage
        {
            static class SchemaManager
            {
                public static async Task EnsureTablesExistAsync(ISqlConnection connectionFactory)
                {
                    using(var connection = connectionFactory.OpenConnection())
                    {
                        await connection.ExecuteNonQueryAsync($@"
IF NOT EXISTS (select name from sys.tables where name = '{OutboxMessages.TableName}')
BEGIN
    CREATE TABLE [dbo].[{OutboxMessages.TableName}]
    (
	    [{OutboxMessages.Identity}] [int] IDENTITY(1,1) NOT NULL,
        [{OutboxMessages.TypeIdGuidValue}] [uniqueidentifier] NOT NULL,
        [{OutboxMessages.MessageId}] [uniqueidentifier] NOT NULL,
	    [{OutboxMessages.Body}] [nvarchar](MAX) NOT NULL,

        CONSTRAINT [PK_{OutboxMessages.TableName}] PRIMARY KEY CLUSTERED 
        (
	        [{OutboxMessages.Identity}] ASC
        ),

        CONSTRAINT IX_{OutboxMessages.TableName}_Unique_{OutboxMessages.MessageId} UNIQUE
        (
            {OutboxMessages.MessageId}
        )

    ) ON [PRIMARY]

    CREATE TABLE [dbo].[{MessageDispatching.TableName}]
    (
	    [{MessageDispatching.MessageId}] [uniqueidentifier] NOT NULL,
        [{MessageDispatching.EndpointId}] [uniqueidentifier] NOT NULL,
        [{MessageDispatching.IsReceived}] [bit] NOT NULL,

        CONSTRAINT [PK_{MessageDispatching.TableName}] 
            PRIMARY KEY CLUSTERED( {MessageDispatching.MessageId}, {MessageDispatching.EndpointId})
            WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF) ON [PRIMARY],

        CONSTRAINT FK_{MessageDispatching.TableName}_{MessageDispatching.MessageId} 
            FOREIGN KEY ( [{MessageDispatching.MessageId}] )  
            REFERENCES {OutboxMessages.TableName} ([{OutboxMessages.MessageId}])

    ) ON [PRIMARY]
END
");
                    }
                }
            }
        }
    }
}
