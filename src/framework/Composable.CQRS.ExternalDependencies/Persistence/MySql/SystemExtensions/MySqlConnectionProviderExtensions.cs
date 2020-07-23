using System;
using System.Threading.Tasks;
using Composable.SystemCE.Reflection.Threading;
using MySql.Data.MySqlClient;

namespace Composable.Persistence.MySql.SystemExtensions
{
    static class MyMySqlConnectionProviderExtensions
    {
        public static int ExecuteNonQuery(this IMySqlConnectionProvider @this, string commandText) => @this.UseCommand(command => command.SetCommandText(commandText).ExecuteNonQuery());

        public static object ExecuteScalar(this IMySqlConnectionProvider @this, string commandText) => @this.UseCommand(command => command.SetCommandText(commandText).ExecuteScalar());

        public static async Task<int> ExecuteNonQueryAsync(this IMySqlConnectionProvider @this, string commandText)
            => await @this.UseConnectionAsync(async connection => await connection.ExecuteNonQueryAsync(commandText).NoMarshalling()).NoMarshalling();

        public static void ExecuteReader(this IMySqlConnectionProvider @this, string commandText, Action<MySqlDataReader> forEach) => @this.UseCommand(command => command.ExecuteReader(commandText, forEach));

        public static void UseCommand(this IMySqlConnectionProvider @this, Action<MySqlCommand> action) => @this.UseConnection(connection => connection.UseCommand(action));

        public static TResult UseCommand<TResult>(this IMySqlConnectionProvider @this, Func<MySqlCommand, TResult> action) => @this.UseConnection(connection => connection.UseCommand(action));
    }
}
