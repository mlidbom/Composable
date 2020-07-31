using System;
using System.Threading.Tasks;
using Npgsql;

namespace Composable.Persistence.PgSql.SystemExtensions
{
    interface INpgsqlConnectionProvider
    {
        TResult UseConnection<TResult>(Func<ComposableNpgsqlConnection, TResult> func);
        Task<TResult> UseConnectionAsync<TResult>(Func<ComposableNpgsqlConnection,Task<TResult>> func);

        void UseConnection(Action<ComposableNpgsqlConnection> action);
        Task UseConnectionAsync(Func<ComposableNpgsqlConnection, Task> action);
    }
}
