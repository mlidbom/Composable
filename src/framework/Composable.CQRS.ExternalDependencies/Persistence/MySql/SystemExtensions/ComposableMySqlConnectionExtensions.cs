using System;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using Composable.SystemCE.ThreadingCE;

namespace Composable.Persistence.MySql.SystemExtensions
{
    static class ComposableMySqlConnectionExtensions
    {
        public static void UseCommand(this ComposableMySqlConnection @this, Action<MySqlCommand> action)
        {
            using var command = @this.CreateCommand();
            action(command);
        }

        public static TResult UseCommand<TResult>(this ComposableMySqlConnection @this, Func<MySqlCommand, TResult> action)
        {
            using var command = @this.CreateCommand();
            return action(command);
        }

        public static void ExecuteNonQuery(this ComposableMySqlConnection @this, string commandText) => @this.UseCommand(command => command.ExecuteNonQuery(commandText));
        public static async Task<int> ExecuteNonQueryAsync(this ComposableMySqlConnection @this, string commandText) => await @this.UseCommand(command => command.ExecuteNonQueryAsync(commandText)).NoMarshalling();
        public static object ExecuteScalar(this ComposableMySqlConnection @this, string commandText) => @this.UseCommand(command => command.ExecuteScalar(commandText));
        public static Task<object> ExecuteScalarAsync(this ComposableMySqlConnection @this, string commandText) => @this.UseCommand(command => command.ExecuteScalarAsync(commandText));
        public static void ExecuteReader(this ComposableMySqlConnection @this, string commandText, Action<MySqlDataReader> forEach) => @this.UseCommand(command => command.ExecuteReader(commandText, forEach));
    }
}
