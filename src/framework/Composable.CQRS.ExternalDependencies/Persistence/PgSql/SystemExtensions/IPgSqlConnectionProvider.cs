using System;
using System.Threading.Tasks;
using Npgsql;

namespace Composable.Persistence.PgSql.SystemExtensions
{
    interface INpgsqlConnectionProvider
    {
        TResult UseConnection<TResult>(Func<NpgsqlConnection, TResult> func);
        Task<TResult> UseConnectionAsync<TResult>(Func<NpgsqlConnection,Task<TResult>> func);

        void UseConnection(Action<NpgsqlConnection> action);
        Task UseConnectionAsync(Func<NpgsqlConnection, Task> action);
    }
}
