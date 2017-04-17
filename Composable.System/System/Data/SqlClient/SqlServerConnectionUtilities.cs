using System;
using System.Data.SqlClient;
using System.Transactions;

namespace Composable.System.Data.SqlClient
{
    class SqlServerConnectionUtilities
    {
        string ConnectionString { get; }
        public SqlServerConnectionUtilities(string connectionString) => ConnectionString = connectionString;

        public void ClearConnectionPool()
        {
            SqlConnection.ClearPool(new SqlConnection(connectionString:ConnectionString));
        }

        public int ExecuteNonQuery(string commandText)
        {
            return UseCommand(
                command =>
                {
                    command.CommandText = commandText;
                    return command.ExecuteNonQuery();
                });
        }

        public object ExecuteScalar(string commandText)
        {
            return UseCommand(
                command =>
                {
                    command.CommandText = commandText;
                    return command.ExecuteScalar();
                });
        }

        public void UseConnection(Action<SqlConnection> action)
        {
            using (var connection = OpenConnection())
            {
                action(connection);
            }
        }

        TResult UseConnection<TResult>(Func<SqlConnection, TResult> action)
        {
            using (var connection = OpenConnection())
            {
                return action(connection);
            }
        }

        public void UseCommand(Action<SqlCommand> action)
        {
            UseConnection(connection =>
                          {
                              using (var command = connection.CreateCommand())
                              {
                                  action(command);
                              }
                          });
        }

        TResult UseCommand<TResult>(Func<SqlCommand, TResult> action)
        {
            return UseConnection(connection =>
                                 {
                                     using (var command = connection.CreateCommand())
                                     {
                                         return action(command);
                                     }
                                 });
        }

        public SqlConnection OpenConnection()
        {
            var transactionInformationDistributedIdentifierBefore = Transaction.Current?.TransactionInformation.DistributedIdentifier;
            var connection = new SqlConnection(ConnectionString);
            connection.Open();
            if(transactionInformationDistributedIdentifierBefore != null && transactionInformationDistributedIdentifierBefore.Value == Guid.Empty)
            {
                if (Transaction.Current.TransactionInformation.DistributedIdentifier != Guid.Empty)
                {
                    throw new Exception($"Opening connection escalated transaction to distributed. For now this is disallowed");
                }
            }
            return connection;
        }
    }
}