using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Composable.Persistence.SqlServer.SystemExtensions
{
    static class SqlServerConnectionProviderExtensions
    {
        public static int ExecuteNonQuery(this ISqlServerConnectionProvider @this, string commandText) => @this.UseCommand(command => command.SetCommandText(commandText).ExecuteNonQuery());

        public static async Task<int> ExecuteNonQueryAsync(this ISqlServerConnectionProvider @this, string commandText)
            => await @this.UseConnectionAsync(async connection => await connection.ExecuteNonQueryAsync(commandText));

        public static object ExecuteScalar(this ISqlServerConnectionProvider @this, string commandText) => @this.UseCommand(command => command.SetCommandText(commandText).ExecuteScalar());

        public static void ExecuteReader(this ISqlServerConnectionProvider @this, string commandText, Action<SqlDataReader> forEach) => @this.UseCommand(command => command.ExecuteReader(commandText, forEach));

        public static void UseCommand(this ISqlServerConnectionProvider @this, Action<SqlCommand> action) => @this.UseConnection(connection => connection.UseCommand(action));

        public static TResult UseCommand<TResult>(this ISqlServerConnectionProvider @this, Func<SqlCommand, TResult> action) => @this.UseConnection(connection => connection.UseCommand(action));
    }
}
