using System;
using System.Threading.Tasks;
using Composable.Persistence.Common;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE;

namespace Composable.Persistence.MsSql.SystemExtensions
{
    class MsSqlConnectionProvider : IMsSqlConnectionProvider
    {
        readonly OptimizedLazy<DbConnectionPool<ComposableMsSqlConnection>> _pool;
        DbConnectionPool<ComposableMsSqlConnection> Pool => _pool.Value;

        public MsSqlConnectionProvider(string connectionString) : this(() => connectionString) {}

        public MsSqlConnectionProvider(Func<string> getConnectionString)
        {
            _pool = new OptimizedLazy<DbConnectionPool<ComposableMsSqlConnection>>(
                () =>
                {
                    var connectionString = getConnectionString();
                    return DbConnectionPool<ComposableMsSqlConnection>.ForConnectionString(
                        connectionString,
                        PoolableConnectionFlags.MustUseSameConnectionThroughoutATransaction,
                        ComposableMsSqlConnection.Create);
                });
        }

        public TResult UseConnection<TResult>(Func<ComposableMsSqlConnection, TResult> func) => Pool.UseConnection(func);

        public void UseConnection(Action<ComposableMsSqlConnection> action) => Pool.UseConnection(action);

        public async Task UseConnectionAsync(Func<ComposableMsSqlConnection, Task> action) => await Pool.UseConnectionAsync(action).NoMarshalling();

        public async Task<TResult> UseConnectionAsync<TResult>(Func<ComposableMsSqlConnection, Task<TResult>> func) => await Pool.UseConnectionAsync(func).NoMarshalling();
    }
}
