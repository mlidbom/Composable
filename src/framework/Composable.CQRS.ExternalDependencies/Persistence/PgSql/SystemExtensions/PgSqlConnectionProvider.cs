using System;
using System.Threading.Tasks;
using Composable.Persistence.Common;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE;

namespace Composable.Persistence.PgSql.SystemExtensions
{
    class PgSqlConnectionProvider : INpgsqlConnectionProvider
    {
        readonly OptimizedLazy<DbConnectionPool<ComposableNpgsqlConnection>> _pool;
        DbConnectionPool<ComposableNpgsqlConnection> Pool => _pool.Value;

        public PgSqlConnectionProvider(string connectionString) : this(() => connectionString) {}

        public PgSqlConnectionProvider(Func<string> getConnectionString)
        {
            _pool = new OptimizedLazy<DbConnectionPool<ComposableNpgsqlConnection>>(
                () =>
                {
                    var connectionString = getConnectionString();
                    return DbConnectionPool<ComposableNpgsqlConnection>.ForConnectionString(
                        connectionString,
                        PoolableConnectionFlags.MustUseSameConnectionThroughoutATransaction,
                        ComposableNpgsqlConnection.Create);
                });
        }

        public TResult UseConnection<TResult>(Func<ComposableNpgsqlConnection, TResult> func) => Pool.UseConnection(func);

        public void UseConnection(Action<ComposableNpgsqlConnection> action) => Pool.UseConnection(action);

        public async Task UseConnectionAsync(Func<ComposableNpgsqlConnection, Task> action) => await Pool.UseConnectionAsync(action).NoMarshalling();

        public async Task<TResult> UseConnectionAsync<TResult>(Func<ComposableNpgsqlConnection, Task<TResult>> func) => await Pool.UseConnectionAsync(func).NoMarshalling();
    }
}
