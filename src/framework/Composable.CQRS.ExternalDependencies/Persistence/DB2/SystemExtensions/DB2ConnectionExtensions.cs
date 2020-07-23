using System;
using IBM.Data.DB2.Core;
using System.Threading.Tasks;
using Composable.SystemCE.ThreadingCE;

namespace Composable.Persistence.DB2.SystemExtensions
{
    static class ComposableDB2ConnectionExtensions
    {
        public static void UseCommand(this ComposableDB2Connection @this, Action<DB2Command> action)
        {
            using var command = @this.CreateCommand();
            action(command);
        }

        public static TResult UseCommand<TResult>(this ComposableDB2Connection @this, Func<DB2Command, TResult> action)
        {
            using var command = @this.CreateCommand();
            return action(command);
        }

        public static async Task UseCommandAsync(this ComposableDB2Connection @this, Func<DB2Command, Task> action)
        {
            using var command = @this.CreateCommand();
            await action(command).NoMarshalling();
        }

        public static void ExecuteNonQuery(this ComposableDB2Connection @this, string commandText) => @this.UseCommand(command => command.ExecuteNonQuery(commandText));
        public static async Task<int> ExecuteNonQueryAsync(this ComposableDB2Connection @this, string commandText) => await @this.UseCommand(command => command.ExecuteNonQueryAsync(commandText)).NoMarshalling();
        public static object ExecuteScalar(this ComposableDB2Connection @this, string commandText) => @this.UseCommand(command => command.ExecuteScalar(commandText));
        public static Task<object> ExecuteScalarAsync(this ComposableDB2Connection @this, string commandText) => @this.UseCommand(command => command.ExecuteScalarAsync(commandText));
        public static void ExecuteReader(this ComposableDB2Connection @this, string commandText, Action<DB2DataReader> forEach) => @this.UseCommand(command => command.ExecuteReader(commandText, forEach));
    }
}
