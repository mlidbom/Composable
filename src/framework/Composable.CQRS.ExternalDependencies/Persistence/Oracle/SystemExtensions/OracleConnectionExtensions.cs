using System;
using Oracle.ManagedDataAccess.Client;
using System.Threading.Tasks;

namespace Composable.Persistence.Oracle.SystemExtensions
{
    static class MyOracleConnectionExtensions
    {
        public static void UseCommand(this OracleConnection @this, Action<OracleCommand> action)
        {
            using var command = @this.CreateCommand();
            command.BindByName = true;
            action(command);
        }

        public static TResult UseCommand<TResult>(this OracleConnection @this, Func<OracleCommand, TResult> action)
        {
            using var command = @this.CreateCommand();
            command.BindByName = true;
            return action(command);
        }

        public static void ExecuteNonQuery(this OracleConnection @this, string commandText) => @this.UseCommand(command => command.ExecuteNonQuery(commandText));
        public static async Task<int> ExecuteNonQueryAsync(this OracleConnection @this, string commandText) => await @this.UseCommand(command => command.ExecuteNonQueryAsync(commandText));
        public static object ExecuteScalar(this OracleConnection @this, string commandText) => @this.UseCommand(command => command.ExecuteScalar(commandText));
        public static Task<object> ExecuteScalarAsync(this OracleConnection @this, string commandText) => @this.UseCommand(command => command.ExecuteScalarAsync(commandText));
        public static void ExecuteReader(this OracleConnection @this, string commandText, Action<OracleDataReader> forEach) => @this.UseCommand(command => command.ExecuteReader(commandText, forEach));
    }
}