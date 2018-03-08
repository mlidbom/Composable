using System.Threading.Tasks;
using Composable.System.Data.SqlClient;

namespace Composable.Messaging.Buses.Implementation
{
    partial class Inbox
    {
        static class InboxMessages
        {
            internal const string TableName = nameof(InboxMessages);

            internal const string Identity = nameof(Identity);
            internal const string TypeId = nameof(TypeId);
            internal const string MessageId = nameof(MessageId);
            internal const string Body = nameof(Body);
            public const string Status = nameof(Status);
            public const string ExceptionCount = nameof(ExceptionCount);
            public const string ExceptionMessage = nameof(ExceptionMessage);
            public const string ExceptionType = nameof(ExceptionType);
            public const string ExceptionStackTrace = nameof(ExceptionStackTrace);
        }

        public enum MessageStatus
        {
            UnHandled = 0,
            Succeeded = 1,
            Failed = 2
        }

        partial class MessageStorage
        {
            static class SchemaManager
            {
                public static async Task EnsureTablesExistAsync(ISqlConnectionProvider connectionFactory)
                {
                    using(var connection = connectionFactory.OpenConnection())
                    {
                            await connection.ExecuteNonQueryAsync($@"
IF NOT EXISTS(select name from sys.tables where name = '{InboxMessages.TableName}')
BEGIN
    CREATE TABLE [dbo].[{InboxMessages.TableName}]
    (
	    [{InboxMessages.Identity}] [int] IDENTITY(1,1) NOT NULL,
        [{InboxMessages.TypeId}] [uniqueidentifier] NOT NULL,
        [{InboxMessages.MessageId}] [uniqueidentifier] NOT NULL,
	    [{InboxMessages.Status}] [smallint]NOT NULL,
	    [{InboxMessages.Body}] [nvarchar](MAX) NOT NULL,
        [{InboxMessages.ExceptionCount}] [int] NOT NULL DEFAULT 0,
        [{InboxMessages.ExceptionType}] [nvarchar](500) NULL,
        [{InboxMessages.ExceptionStackTrace}] [nvarchar](MAX) NULL,
        [{InboxMessages.ExceptionMessage}] [nvarchar](MAX) NULL,


        CONSTRAINT [PK_{InboxMessages.TableName}] PRIMARY KEY CLUSTERED 
        (
	        [{InboxMessages.Identity}] ASC
        ),

        CONSTRAINT IX_{InboxMessages.TableName}_Unique_{InboxMessages.MessageId} UNIQUE
        (
            {InboxMessages.MessageId}
        )

    ) ON [PRIMARY]
END
");
                    }
                }
            }
        }
    }
}
