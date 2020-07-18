using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Composable.System.Threading;

namespace Composable.Persistence.MsSql.SystemExtensions
{
    static class MsSqlConnectionProviderExtensions
    {
        public static int ExecuteNonQuery(this IMsSqlConnectionProvider @this, string commandText) => @this.UseCommand(command => command.SetCommandText(commandText).ExecuteNonQuery());

        public static async Task<int> ExecuteNonQueryAsync(this IMsSqlConnectionProvider @this, string commandText)
            => await @this.UseConnectionAsync(async connection => await connection.ExecuteNonQueryAsync(commandText).NoMarshalling()).NoMarshalling();

        public static object ExecuteScalar(this IMsSqlConnectionProvider @this, string commandText) => @this.UseCommand(command => command.SetCommandText(commandText).ExecuteScalar());

        public static void ExecuteReader(this IMsSqlConnectionProvider @this, string commandText, Action<SqlDataReader> forEach) => @this.UseCommand(command => command.ExecuteReader(commandText, forEach));

        public static void UseCommand(this IMsSqlConnectionProvider @this, Action<SqlCommand> action) => @this.UseConnection(connection => connection.UseCommand(action));

        public static TResult UseCommand<TResult>(this IMsSqlConnectionProvider @this, Func<SqlCommand, TResult> action) => @this.UseConnection(connection => connection.UseCommand(action));
    }
}
