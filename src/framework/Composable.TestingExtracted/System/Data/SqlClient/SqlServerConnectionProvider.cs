using System;
using System.Data.SqlClient;
using System.Transactions;

namespace Composable.Testing.System.Data.SqlClient
{
    class SqlServerConnection : ISqlConnection
    {
        public string ConnectionString { get; }

        public SqlServerConnection(string connectionString) => ConnectionString = connectionString;

        public SqlConnection OpenConnection()
        {
            var transactionInformationDistributedIdentifierBefore = Transaction.Current?.TransactionInformation.DistributedIdentifier;
            var connection = new SqlConnection(ConnectionString);
            connection.Open();
            if(transactionInformationDistributedIdentifierBefore != null && transactionInformationDistributedIdentifierBefore.Value == Guid.Empty)
            {
                if (Transaction.Current.TransactionInformation.DistributedIdentifier != Guid.Empty)
                {
                    throw new Exception("Opening connection escalated transaction to distributed. For now this is disallowed");
                }
            }
            return connection;
        }
    }

    public static class SqlConnectionProviderExtensions
    {
        public static int ExecuteNonQuery(this ISqlConnection @this, string commandText)
        {
            return @this.UseCommand(
                command =>
                {
                    command.CommandText = commandText;
                    return command.ExecuteNonQuery();
                });
        }

        public static object ExecuteScalar(this ISqlConnection @this, string commandText)
        {
            return @this.UseCommand(
                command =>
                {
                    command.CommandText = commandText;
                    return command.ExecuteScalar();
                });
        }

        public static void UseConnection(this ISqlConnection @this, Action<SqlConnection> action)
        {
            using (var connection = @this.OpenConnection())
            {
                action(connection);
            }
        }

        static TResult UseConnection<TResult>(this ISqlConnection @this, Func<SqlConnection, TResult> action)
        {
            using (var connection = @this.OpenConnection())
            {
                return action(connection);
            }
        }

        public static void UseCommand(this ISqlConnection @this, Action<SqlCommand> action)
        {
            @this.UseConnection(connection =>
                          {
                              using (var command = connection.CreateCommand())
                              {
                                  action(command);
                              }
                          });
        }

        static TResult UseCommand<TResult>(this ISqlConnection @this, Func<SqlCommand, TResult> action)
        {
            return @this.UseConnection(connection =>
                                 {
                                     using (var command = connection.CreateCommand())
                                     {
                                         return action(command);
                                     }
                                 });
        }
    }

    public interface ISqlConnection
    {
        SqlConnection OpenConnection();
        string ConnectionString { get; }
    }
}