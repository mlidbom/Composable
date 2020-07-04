using System;
using MySql.Data.MySqlClient;

namespace Composable.Persistence.MySql.SystemExtensions
{
    static class MyMySqlConnectionProviderExtensions
    {
        public static int ExecuteNonQuery(this IMySqlConnectionProvider @this, string commandText) => @this.UseCommand(command => command.SetCommandText(commandText).ExecuteNonQuery());

        public static object ExecuteScalar(this IMySqlConnectionProvider @this, string commandText) => @this.UseCommand(command => command.SetCommandText(commandText).ExecuteScalar());

        public static void ExecuteReader(this IMySqlConnectionProvider @this, string commandText, Action<MySqlDataReader> forEach) => @this.UseCommand(command => command.ExecuteReader(commandText, forEach));

        public static void UseConnection(this IMySqlConnectionProvider @this, Action<MySqlConnection> action)
        {
            using var connection = @this.OpenConnection();
            action(connection);
        }

        static TResult UseConnection<TResult>(this IMySqlConnectionProvider @this, Func<MySqlConnection, TResult> action)
        {
            using var connection = @this.OpenConnection();
            return action(connection);
        }

        public static void UseCommand(this IMySqlConnectionProvider @this, Action<MySqlCommand> action) => @this.UseConnection(connection => connection.UseCommand(action));

        public static TResult UseCommand<TResult>(this IMySqlConnectionProvider @this, Func<MySqlCommand, TResult> action) => @this.UseConnection(connection => connection.UseCommand(action));
    }
}
