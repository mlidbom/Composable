using System;
using System.Threading.Tasks;
using Composable.SystemCE.Reflection.Threading;
using Oracle.ManagedDataAccess.Client;

namespace Composable.Persistence.Oracle.SystemExtensions
{
    static class OracleConnectionProviderExtensions
    {
        public static int ExecuteNonQuery(this IOracleConnectionProvider @this, string commandText) => @this.UseCommand(command => command.SetCommandText(commandText).ExecuteNonQuery());

        public static object ExecuteScalar(this IOracleConnectionProvider @this, string commandText) => @this.UseCommand(command => command.SetCommandText(commandText).ExecuteScalar());

        public static async Task<int> ExecuteNonQueryAsync(this IOracleConnectionProvider @this, string commandText)
            => await @this.UseConnectionAsync(async connection => await connection.ExecuteNonQueryAsync(commandText).NoMarshalling()).NoMarshalling();

        public static void ExecuteReader(this IOracleConnectionProvider @this, string commandText, Action<OracleDataReader> forEach) => @this.UseCommand(command => command.ExecuteReader(commandText, forEach));

        public static void UseCommand(this IOracleConnectionProvider @this, Action<OracleCommand> action) => @this.UseConnection(connection => connection.UseCommand(action));

        public static Task UseCommandAsync(this IOracleConnectionProvider @this, Func<OracleCommand, Task> action) => @this.UseConnectionAsync(async connection => await connection.UseCommandAsync(action).NoMarshalling());

        public static TResult UseCommand<TResult>(this IOracleConnectionProvider @this, Func<OracleCommand, TResult> action) => @this.UseConnection(connection => connection.UseCommand(action));
    }
}
