using System;
using Npgsql;
using System.Threading.Tasks;
using Composable.SystemCE.Reflection.Threading;

namespace Composable.Persistence.PgSql.SystemExtensions
{
    static class MyNpgsqlConnectionExtensions
    {
        public static void UseCommand(this NpgsqlConnection @this, Action<NpgsqlCommand> action)
        {
            using var command = @this.CreateCommand();
            action(command);
        }

        public static TResult UseCommand<TResult>(this NpgsqlConnection @this, Func<NpgsqlCommand, TResult> action)
        {
            using var command = @this.CreateCommand();
            return action(command);
        }

        public static void ExecuteNonQuery(this NpgsqlConnection @this, string commandText) => @this.UseCommand(command => command.ExecuteNonQuery(commandText));
        public static async Task<int> ExecuteNonQueryAsync(this NpgsqlConnection @this, string commandText) => await @this.UseCommand(command => command.ExecuteNonQueryAsync(commandText)).NoMarshalling();
        public static object ExecuteScalar(this NpgsqlConnection @this, string commandText) => @this.UseCommand(command => command.ExecuteScalar(commandText));
        public static Task<object> ExecuteScalarAsync(this NpgsqlConnection @this, string commandText) => @this.UseCommand(command => command.ExecuteScalarAsync(commandText));
        public static void ExecuteReader(this NpgsqlConnection @this, string commandText, Action<NpgsqlDataReader> forEach) => @this.UseCommand(command => command.ExecuteReader(commandText, forEach));
    }
}
