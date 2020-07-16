using System;
using IBM.Data.DB2.Core;
using System.Threading.Tasks;

namespace Composable.Persistence.DB2.SystemExtensions
{
    static class DB2ConnectionExtensions
    {
        public static void UseCommand(this DB2Connection @this, Action<DB2Command> action)
        {
            using var command = @this.CreateCommand();
            action(command);
        }

        public static TResult UseCommand<TResult>(this DB2Connection @this, Func<DB2Command, TResult> action)
        {
            using var command = @this.CreateCommand();
            return action(command);
        }

        public static async Task UseCommandAsync(this DB2Connection @this, Func<DB2Command, Task> action)
        {
            using var command = @this.CreateCommand();
            await action(command);
        }

        public static void ExecuteNonQuery(this DB2Connection @this, string commandText) => @this.UseCommand(command => command.ExecuteNonQuery(commandText));
        public static async Task<int> ExecuteNonQueryAsync(this DB2Connection @this, string commandText) => await @this.UseCommand(command => command.ExecuteNonQueryAsync(commandText));
        public static object ExecuteScalar(this DB2Connection @this, string commandText) => @this.UseCommand(command => command.ExecuteScalar(commandText));
        public static Task<object> ExecuteScalarAsync(this DB2Connection @this, string commandText) => @this.UseCommand(command => command.ExecuteScalarAsync(commandText));
        public static void ExecuteReader(this DB2Connection @this, string commandText, Action<DB2DataReader> forEach) => @this.UseCommand(command => command.ExecuteReader(commandText, forEach));
    }
}
