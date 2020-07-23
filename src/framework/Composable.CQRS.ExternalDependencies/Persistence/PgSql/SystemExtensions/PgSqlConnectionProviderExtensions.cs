using System;
using System.Threading.Tasks;
using Composable.SystemCE.Reflection.Threading;
using Npgsql;

namespace Composable.Persistence.PgSql.SystemExtensions
{
    static class MyNpgsqlConnectionProviderExtensions
    {
        public static int ExecuteNonQuery(this INpgsqlConnectionProvider @this, string commandText) => @this.UseCommand(command => command.SetCommandText(commandText).ExecuteNonQuery());

        public static object ExecuteScalar(this INpgsqlConnectionProvider @this, string commandText) => @this.UseCommand(command => command.SetCommandText(commandText).ExecuteScalar());

        public static async Task<int> ExecuteNonQueryAsync(this INpgsqlConnectionProvider @this, string commandText)
            => await @this.UseConnectionAsync(async connection => await connection.ExecuteNonQueryAsync(commandText).NoMarshalling()).NoMarshalling();

        public static void ExecuteReader(this INpgsqlConnectionProvider @this, string commandText, Action<NpgsqlDataReader> forEach) => @this.UseCommand(command => command.ExecuteReader(commandText, forEach));

        public static void UseCommand(this INpgsqlConnectionProvider @this, Action<NpgsqlCommand> action) => @this.UseConnection(connection => connection.UseCommand(action));

        public static TResult UseCommand<TResult>(this INpgsqlConnectionProvider @this, Func<NpgsqlCommand, TResult> action) => @this.UseConnection(connection => connection.UseCommand(action));
    }
}
