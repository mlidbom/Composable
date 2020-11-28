using System;
using System.Threading.Tasks;
using Composable.Persistence.Common.AdoCE;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE;
using Npgsql;

namespace Composable.Persistence.PgSql.SystemExtensions
{
    interface IPgSqlConnectionPool : IDbConnectionPool<IComposableNpgsqlConnection, NpgsqlCommand>
    {
        internal static IPgSqlConnectionPool CreateInstance1(Func<string> getConnectionString) => new PgSqlConnectionPool(getConnectionString);
        internal static IPgSqlConnectionPool CreateInstance(string connectionString) => new PgSqlConnectionPool(connectionString);

        class PgSqlConnectionPool : IPgSqlConnectionPool
        {
            readonly OptimizedLazy<IDbConnectionPool<IComposableNpgsqlConnection, NpgsqlCommand>> _pool;
            IDbConnectionPool<IComposableNpgsqlConnection, NpgsqlCommand> Pool => _pool.Value;

            public PgSqlConnectionPool(string connectionString) : this(() => connectionString) {}

            public PgSqlConnectionPool(Func<string> getConnectionString)
            {
                _pool = new OptimizedLazy<IDbConnectionPool<IComposableNpgsqlConnection, NpgsqlCommand>>(
                    () =>
                    {
                        var connectionString = getConnectionString();
                        return DbConnectionManager<IComposableNpgsqlConnection, NpgsqlCommand>.ForConnectionString(
                            connectionString,
                            PoolableConnectionFlags.MustUseSameConnectionThroughoutATransaction,
                            IComposableNpgsqlConnection.Create);
                    });
            }

            public Task<TResult> UseConnectionAsyncFlex<TResult>(SyncOrAsync syncOrAsync, Func<IComposableNpgsqlConnection, Task<TResult>> func) =>
                Pool.UseConnectionAsyncFlex(syncOrAsync, func);
        }
    }
}
