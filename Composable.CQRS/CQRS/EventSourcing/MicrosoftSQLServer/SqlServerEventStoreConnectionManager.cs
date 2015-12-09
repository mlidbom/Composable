using System;
using System.Data.SqlClient;
using System.Transactions;
using Composable.Logging.Log4Net;

namespace Composable.CQRS.EventSourcing.MicrosoftSQLServer
{
    internal class SqlServerEventStoreConnectionManager
    {
        private string ConnectionString { get; }
        public SqlServerEventStoreConnectionManager(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public void UseConnection(Action<SqlConnection> action, bool suppressTransactionWarning = false)
        {
            using(var connection = OpenConnection(suppressTransactionWarning))
            {
                action(connection);
            }
        }

        public TResult UseConnection<TResult>(Func<SqlConnection, TResult> action, bool suppressTransactionWarning = false)
        {
            using (var connection = OpenConnection(suppressTransactionWarning))
            {
                return action(connection);
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
                          });
        }

        public TResult UseCommand<TResult>(Func<SqlCommand, TResult> action, bool suppressTransactionWarning = false)
        {
            return UseConnection(connection => 
            {
                using (var command = connection.CreateCommand())
                {
                    return action(command);
                }
            });
        }

        public SqlConnection OpenConnection(bool suppressTransactionWarning = false)
        {
            var connection = new SqlConnection(ConnectionString);
            connection.Open();
            if (!suppressTransactionWarning && Transaction.Current == null)
            {
                this.Log().Warn($@"No ambient transaction. This is dangerous:
AT: 

{Environment.StackTrace}");
            }
            return connection;
        }
    }
}