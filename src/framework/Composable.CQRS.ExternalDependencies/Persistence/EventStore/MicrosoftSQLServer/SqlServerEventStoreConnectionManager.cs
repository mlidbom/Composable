using System;
using System.Data.SqlClient;
using System.Transactions;
using Composable.Logging;
using Composable.System.Data.SqlClient;

namespace Composable.Persistence.EventStore.MicrosoftSQLServer
{
    class SqlServerEventStoreConnectionManager
    {
        readonly ISqlConnection _connectionManager;
        public SqlServerEventStoreConnectionManager(ISqlConnection connectionString) => _connectionManager = connectionString;

        void UseConnection(Action<SqlConnection> action, bool suppressTransactionWarning = false)
        {
            using(var connection = OpenConnection(suppressTransactionWarning))
            {
                action(connection);
            }
        }

        public void UseCommand(Action<SqlCommand> action, bool suppressTransactionWarning = false)
        {
            UseConnection(connection =>
                          {
                              using(var command = connection.CreateCommand())
                              {
                                  action(command);
                              }
                          }, suppressTransactionWarning);
        }

        public SqlConnection OpenConnection(bool suppressTransactionWarning = false)
        {
            if (!suppressTransactionWarning && Transaction.Current == null)
            {
                throw new Exception("You must use a transaction to make modifications to the event store.");
            }
            return _connectionManager.OpenConnection();
        }
    }
}