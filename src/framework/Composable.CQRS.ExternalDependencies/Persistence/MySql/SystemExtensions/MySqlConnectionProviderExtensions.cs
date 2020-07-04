using System;
using System.Data.SqlClient;

namespace Composable.Persistence.MySql.SystemExtensions
{
    static class SqlServerConnectionProviderExtensions
    {
        public static int ExecuteNonQuery(this ISqlServerConnectionProvider @this, string commandText) => @this.UseCommand(command => command.SetCommandText(commandText).ExecuteNonQuery());

        public static object ExecuteScalar(this ISqlServerConnectionProvider @this, string commandText) => @this.UseCommand(command => command.SetCommandText(commandText).ExecuteScalar());

        public static void ExecuteReader(this ISqlServerConnectionProvider @this, string commandText, Action<SqlDataReader> forEach) => @this.UseCommand(command => command.ExecuteReader(commandText, forEach));

        public static void UseConnection(this ISqlServerConnectionProvider @this, Action<SqlConnection> action)
        {
            using var connection = @this.OpenConnection();
            action(connection);
        }

        static TResult UseConnection<TResult>(this ISqlServerConnectionProvider @this, Func<SqlConnection, TResult> action)
        {
            using var connection = @this.OpenConnection();
            return action(connection);
        }

        public static void UseCommand(this ISqlServerConnectionProvider @this, Action<SqlCommand> action) => @this.UseConnection(connection => connection.UseCommand(action));

        public static TResult UseCommand<TResult>(this ISqlServerConnectionProvider @this, Func<SqlCommand, TResult> action) => @this.UseConnection(connection => connection.UseCommand(action));
    }
}
