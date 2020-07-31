using System;
using Oracle.ManagedDataAccess.Client;
using System.Threading.Tasks;
using Composable.SystemCE.ThreadingCE;

namespace Composable.Persistence.Oracle.SystemExtensions
{
    static class OracleConnectionExtensions
    {
        public static void UseCommand(this ComposableOracleConnection @this, Action<OracleCommand> action)
        {
            using var command = @this.CreateCommand();
            command.BindByName = true;
            action(command);
        }

        public static TResult UseCommand<TResult>(this ComposableOracleConnection @this, Func<OracleCommand, TResult> action)
        {
            using var command = @this.CreateCommand();
            command.BindByName = true;
            return action(command);
        }

        public static async Task UseCommandAsync(this ComposableOracleConnection @this, Func<OracleCommand, Task> action)
        {
            using var command = @this.CreateCommand();
            command.BindByName = true;
            await action(command).NoMarshalling();
        }

        public static void ExecuteNonQuery(this ComposableOracleConnection @this, string commandText) => @this.UseCommand(command => command.ExecuteNonQuery(commandText));
        public static async Task<int> ExecuteNonQueryAsync(this ComposableOracleConnection @this, string commandText) => await @this.UseCommand(command => command.ExecuteNonQueryAsync(commandText)).NoMarshalling();
        public static object ExecuteScalar(this ComposableOracleConnection @this, string commandText) => @this.UseCommand(command => command.ExecuteScalar(commandText));
        public static Task<object> ExecuteScalarAsync(this ComposableOracleConnection @this, string commandText) => @this.UseCommand(command => command.ExecuteScalarAsync(commandText));
        public static void ExecuteReader(this ComposableOracleConnection @this, string commandText, Action<OracleDataReader> forEach) => @this.UseCommand(command => command.ExecuteReader(commandText, forEach));
    }
}
