using System.Collections.Generic;
using System.Data.SqlClient;
using System.Transactions;
using Composable.Logging.Log4Net;

namespace Composable.CQRS.EventSourcing.SQLServer
{
    public class SqlServerEventStoreSchemaManager
    {
        private static readonly HashSet<string> VerifiedTables = new HashSet<string>();


        public SqlServerEventStoreSchemaManager(string connectionString)
        {
            ConnectionString = connectionString;
        }

        private string ConnectionString { get; set; }

        private SqlConnection OpenConnection(bool suppressTransactionWarning = false)
        {
            var connection = new SqlConnection(ConnectionString);
            connection.Open();
            if(!suppressTransactionWarning && Transaction.Current == null)
            {
                this.Log().Warn("No ambient transaction. This is dangerous");
            }
            return connection;
        }

        //todo:Move this and its cousins below into another abstraction, delegate to that abstraction, and obsolete these methods.
        public void EnsureEventsTableExists()
        {
            lock(VerifiedTables)
            {
                if(!VerifiedTables.Contains(ConnectionString))
                {
                    using(var connection = OpenConnection())
                    {
                        EnsureEventsTableExists(connection);
                        EnsureEventTypesTableExists(connection);
                        VerifiedTables.Add(ConnectionString);
                    }
                }
            }
        }

        private static void EnsureEventTypesTableExists(SqlConnection connection)
        {
            int exists;
            using (var checkForTableCommand = connection.CreateCommand())
            {
                checkForTableCommand.CommandText = "select count(*) from sys.tables where name = 'EventTypes'";
                exists = (int)checkForTableCommand.ExecuteScalar();
            }
            if (exists == 0)
            {
                using (var createTableCommand = connection.CreateCommand())
                {
                    createTableCommand.CommandText =
@"
    select identity(int, 1,1) as Id, EventType
    into EventType
    from Events
    group by EventType
    order by EventType
";
                    createTableCommand.ExecuteNonQuery();
                }
            }
        }

        private static void EnsureEventsTableExists(SqlConnection connection)
        {
            int exists;
            using(var checkForTableCommand = connection.CreateCommand())
            {
                checkForTableCommand.CommandText = "select count(*) from sys.tables where name = 'Events'";
                exists = (int)checkForTableCommand.ExecuteScalar();
            }
            if(exists == 0)
            {
                using(var createTableCommand = connection.CreateCommand())
                {
                    createTableCommand.CommandText =
                        @"
CREATE TABLE [dbo].[Events](
	[AggregateId] [uniqueidentifier] NOT NULL,
	[AggregateVersion] [int] NOT NULL,
	[TimeStamp] [datetime] NOT NULL,
	[SqlTimeStamp] [timestamp] NOT NULL,
	[EventType] [varchar](300) NOT NULL,
	[EventId] [uniqueidentifier] NOT NULL,
	[Event] [nvarchar](max) NOT NULL,
    [TestBlah] [nvarchar](50) NULL,
CONSTRAINT [IX_Uniq_EventId] UNIQUE
(
	EventId
),
CONSTRAINT [PK_Events] PRIMARY KEY CLUSTERED 
(
	[AggregateId] ASC,
	[AggregateVersion] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF) ON [PRIMARY]
) ON [PRIMARY]
CREATE UNIQUE NONCLUSTERED INDEX [SqlTimeStamp] ON [dbo].[Events]
(
	[SqlTimeStamp] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
";
                    createTableCommand.ExecuteNonQuery();
                }
            }
        }

        public static void ResetDB(string connectionString)
        {
            new SqlServerEventStoreSchemaManager(connectionString).ResetDB();
        }

        public void ResetDB()
        {
            lock(VerifiedTables)
            {
                using(var connection = OpenConnection())
                {
                    DropEventsTable(connection);
                    DropEventTypeTable(connection);
                }
                EnsureEventsTableExists();
            }
        }

        private void DropEventsTable(SqlConnection connection)
        {
            using(var dropCommand = connection.CreateCommand())
            {
                dropCommand.CommandText =
                    @"IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Events]') AND type in (N'U'))
DROP TABLE [dbo].[Events]";

                dropCommand.ExecuteNonQuery();
                VerifiedTables.Remove(ConnectionString);
            }
        }

        private void DropEventTypeTable(SqlConnection connection)
        {
            using (var dropCommand = connection.CreateCommand())
            {
                dropCommand.CommandText =
                    @"IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EventType]') AND type in (N'U'))
DROP TABLE [dbo].[EventType]";

                dropCommand.ExecuteNonQuery();
                VerifiedTables.Remove(ConnectionString);
            }
        }
    }
}