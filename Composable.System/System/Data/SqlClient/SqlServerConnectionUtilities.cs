using System;
using System.Data.SqlClient;

namespace Composable.System.Data.SqlClient
{
    public class SqlServerConnectionUtilities
    {
        string ConnectionString { get; }
        public SqlServerConnectionUtilities(string connectionString)
        {
            ConnectionString = connectionString;
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

        public TResult UseConnection<TResult>(Func<SqlConnection, TResult> action)
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

        public TResult UseCommand<TResult>(Func<SqlCommand, TResult> action)
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
            var connection = new SqlConnection(ConnectionString);
            connection.Open();
            return connection;
        }
    }
}