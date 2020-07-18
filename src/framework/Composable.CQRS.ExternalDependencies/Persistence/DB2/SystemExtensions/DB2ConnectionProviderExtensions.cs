using System;
using System.Threading.Tasks;
using Composable.System.Threading;
using IBM.Data.DB2.Core;

namespace Composable.Persistence.DB2.SystemExtensions
{
    static class ComposableDB2ConnectionProviderExtensions
    {
        public static int ExecuteNonQuery(this IComposableDB2ConnectionProvider @this, string commandText) => @this.UseCommand(command => command.SetCommandText(commandText).ExecuteNonQuery());

        public static object ExecuteScalar(this IComposableDB2ConnectionProvider @this, string commandText) => @this.UseCommand(command => command.SetCommandText(commandText).ExecuteScalar());

        public static async Task<int> ExecuteNonQueryAsync(this IComposableDB2ConnectionProvider @this, string commandText)
            => await @this.UseConnectionAsync(async connection => await connection.ExecuteNonQueryAsync(commandText).NoMarshalling()).NoMarshalling();

        public static void ExecuteReader(this IComposableDB2ConnectionProvider @this, string commandText, Action<DB2DataReader> forEach) => @this.UseCommand(command => command.ExecuteReader(commandText, forEach));

        public static void UseCommand(this IComposableDB2ConnectionProvider @this, Action<DB2Command> action) => @this.UseConnection(connection => connection.UseCommand(action));

        public static Task UseCommandAsync(this IComposableDB2ConnectionProvider @this, Func<DB2Command, Task> action) => @this.UseConnectionAsync(async connection => await connection.UseCommandAsync(action).NoMarshalling());

        public static TResult UseCommand<TResult>(this IComposableDB2ConnectionProvider @this, Func<DB2Command, TResult> action) => @this.UseConnection(connection => connection.UseCommand(action));
    }
}
