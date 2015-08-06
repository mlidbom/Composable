using System.Collections.Generic;
using System.Data.SqlClient;
using System.Transactions;
using Composable.Logging.Log4Net;

namespace Composable.CQRS.EventSourcing.SQLServer
{
    public class SqlServerEventStoreSchemaManager
    {
        private static readonly HashSet<string> VerifiedConnectionStrings = new HashSet<string>();
        private static EventTable EventTable { get; } = new EventTable();
        private static EventTypeTable EventTypeTable { get; } = new EventTypeTable();

        public SqlServerEventStoreSchemaManager(string connectionString)
        {
            ConnectionString = connectionString;
        }

        private string ConnectionString { get; set; }

        private SqlConnection OpenConnection()
        {
            var connection = new SqlConnection(ConnectionString);
            connection.Open();
            if(Transaction.Current == null)
            {
                this.Log().Warn("No ambient transaction. This is dangerous");
            }
            return connection;
        }

        public void SetupSchemaIfDatabaseUnInitialized()
        {
            lock(VerifiedConnectionStrings)
            {
                if(!VerifiedConnectionStrings.Contains(ConnectionString))
                {
                    using(var connection = OpenConnection())
                    {
                        if(!EventTable.Exists(connection))
                        {
                            EventTable.Create(connection);
                            EventTypeTable.Create(connection);
                        }
                        VerifiedConnectionStrings.Add(ConnectionString);
                    }
                }
            }
        }        

        public static void ResetDB(string connectionString)
        {
            new SqlServerEventStoreSchemaManager(connectionString).ResetDB();
        }

        public void ResetDB()
        {
            lock(VerifiedConnectionStrings)
            {
                using(var connection = OpenConnection())
                {
                    EventTable.DropIfExists(connection);
                    EventTypeTable.DropIfExists(connection);
                }
                VerifiedConnectionStrings.Remove(ConnectionString);
                SetupSchemaIfDatabaseUnInitialized();
            }
        }               
    }
}