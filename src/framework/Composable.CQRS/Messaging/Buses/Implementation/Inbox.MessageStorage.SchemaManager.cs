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
            public const string IsHandled = nameof(IsHandled);
        }

        partial class MessageStorage
        {
            static class SchemaManager
            {
                public static void EnsureTablesExist(ISqlConnection connectionFactory)
                {
                    using(var connection = connectionFactory.OpenConnection())
                    {
                        var schemaExists = (int)connection.ExecuteScalar($"select count(*) from sys.tables where name = '{InboxMessages.TableName}'");
                        if(schemaExists == 0)
                        {
                            connection.ExecuteNonQuery($@"
CREATE TABLE [dbo].[{InboxMessages.TableName}]
(
	[{InboxMessages.Identity}] [int] IDENTITY(1,1) NOT NULL,
    [{InboxMessages.TypeId}] [uniqueidentifier] NOT NULL,
    [{InboxMessages.MessageId}] [uniqueidentifier] NOT NULL,
	[{InboxMessages.IsHandled}] [bit]NOT NULL,
	[{InboxMessages.Body}] [nvarchar](MAX) NOT NULL,


    CONSTRAINT [PK_{InboxMessages.TableName}] PRIMARY KEY CLUSTERED 
    (
	    [{InboxMessages.Identity}] ASC
    ),

    CONSTRAINT IX_{InboxMessages.TableName}_Unique_{InboxMessages.MessageId} UNIQUE
    (
        {InboxMessages.MessageId}
    )

) ON [PRIMARY]");
                        }
                    }
                }
            }
        }
    }
}
