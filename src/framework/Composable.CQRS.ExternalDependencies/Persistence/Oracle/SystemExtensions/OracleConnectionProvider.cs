using System;
using System.Threading.Tasks;
using Composable.Persistence.Common;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE;

namespace Composable.Persistence.Oracle.SystemExtensions
{
    class OracleConnectionProvider : IOracleConnectionProvider
    {
        readonly OptimizedLazy<DbConnectionPool<ComposableOracleConnection>> _pool;
        DbConnectionPool<ComposableOracleConnection> Pool => _pool.Value;

        public OracleConnectionProvider(string connectionString) : this(() => connectionString) {}

        public OracleConnectionProvider(Func<string> getConnectionString)
        {
            _pool = new OptimizedLazy<DbConnectionPool<ComposableOracleConnection>>(
                () =>
                {
                    var connectionString = getConnectionString();
                    return DbConnectionPool<ComposableOracleConnection>.ForConnectionString(
                        connectionString,
                        PoolableConnectionFlags.MustUseSameConnectionThroughoutATransaction,
                        ComposableOracleConnection.Create);
                });
        }

        public TResult UseConnection<TResult>(Func<ComposableOracleConnection, TResult> func) => Pool.UseConnection(func);

        public void UseConnection(Action<ComposableOracleConnection> action) => Pool.UseConnection(action);

        public async Task UseConnectionAsync(Func<ComposableOracleConnection, Task> action) => await Pool.UseConnectionAsync(action).NoMarshalling();

        public async Task<TResult> UseConnectionAsync<TResult>(Func<ComposableOracleConnection, Task<TResult>> func) => await Pool.UseConnectionAsync(func).NoMarshalling();
    }
}
