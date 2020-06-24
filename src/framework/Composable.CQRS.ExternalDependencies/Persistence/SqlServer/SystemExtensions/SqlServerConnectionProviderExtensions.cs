using System;
using System.Data.SqlClient;

namespace Composable.Persistence.SqlServer.SystemExtensions
{
    static class SqlServerConnectionProviderExtensions
    {
        public static int ExecuteNonQuery(this ISqlConnectionProvider @this, string commandText) => @this.UseCommand(command => command.SetCommandText(commandText).ExecuteNonQuery());

        public static object ExecuteScalar(this ISqlConnectionProvider @this, string commandText) => @this.UseCommand(command => command.SetCommandText(commandText).ExecuteScalar());

        public static void ExecuteReader(this ISqlConnectionProvider @this, string commandText, Action<SqlDataReader> forEach) => @this.UseCommand(command => command.ExecuteReader(commandText, forEach));

        public static void UseConnection(this ISqlConnectionProvider @this, Action<SqlConnection> action)
        {
            using var connection = @this.OpenConnection();
            action(connection);
        }

        static TResult UseConnection<TResult>(this ISqlConnectionProvider @this, Func<SqlConnection, TResult> action)
        {
            using var connection = @this.OpenConnection();
            return action(connection);
        }

        public static void UseCommand(this ISqlConnectionProvider @this, Action<SqlCommand> action) => @this.UseConnection(connection => connection.UseCommand(action));

        public static TResult UseCommand<TResult>(this ISqlConnectionProvider @this, Func<SqlCommand, TResult> action) => @this.UseConnection(connection => connection.UseCommand(action));
    }
}
