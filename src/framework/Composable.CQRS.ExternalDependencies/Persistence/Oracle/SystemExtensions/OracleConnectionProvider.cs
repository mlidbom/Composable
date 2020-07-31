using System;
using System.Threading.Tasks;
using Composable.Persistence.Common;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE;
using Oracle.ManagedDataAccess.Client;

namespace Composable.Persistence.Oracle.SystemExtensions
{
    interface IOracleConnectionProvider : IDbConnectionPool<IComposableOracleConnection, OracleCommand> {}

    class OracleConnectionProvider : IOracleConnectionProvider
    {
        readonly OptimizedLazy<IDbConnectionPool<IComposableOracleConnection, OracleCommand>> _pool;

        public static IOracleConnectionProvider CreateInstance(string connectionString) => new OracleConnectionProvider(connectionString);
        IDbConnectionPool<IComposableOracleConnection, OracleCommand> Pool => _pool.Value;

        OracleConnectionProvider(string connectionString) : this(() => connectionString) {}

        public OracleConnectionProvider(Func<string> getConnectionString)
        {
            _pool = new OptimizedLazy<IDbConnectionPool<IComposableOracleConnection, OracleCommand>>(
                () =>
                {
                    var connectionString = getConnectionString();
                    return DbConnectionPool<IComposableOracleConnection, OracleCommand>.ForConnectionString(
                        connectionString,
                        PoolableConnectionFlags.Defaults,
                        ComposableOracleConnection.Create);
                });
        }

        public Task<TResult> UseConnectionAsyncFlex<TResult>(AsyncMode syncOrAsync, Func<IComposableOracleConnection, Task<TResult>> func) =>
            Pool.UseConnectionAsyncFlex(syncOrAsync, func);
    }
}
