using System;
using System.Threading.Tasks;
using Composable.Persistence.Common.AdoCE;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE;
using Oracle.ManagedDataAccess.Client;

namespace Composable.Persistence.Oracle.SystemExtensions
{
    interface IOracleConnectionPool : IDbConnectionPool<IComposableOracleConnection, OracleCommand>
    {
        public static IOracleConnectionPool CreateInstance(string connectionString) => CreateInstance(() => connectionString);
        public static OracleConnectionPool CreateInstance(Func<string> getConnectionString) => new OracleConnectionPool(getConnectionString);

        class OracleConnectionPool : IOracleConnectionPool
        {
            readonly OptimizedLazy<IDbConnectionPool<IComposableOracleConnection, OracleCommand>> _pool;

            internal OracleConnectionPool(Func<string> getConnectionString)
            {
                _pool = new OptimizedLazy<IDbConnectionPool<IComposableOracleConnection, OracleCommand>>(
                    () =>
                    {
                        var connectionString = getConnectionString();
                        return DbConnectionManager<IComposableOracleConnection, OracleCommand>.ForConnectionString(
                            connectionString,
                            PoolableConnectionFlags.Defaults,
                            IComposableOracleConnection.Create);
                    });
            }

            public Task<TResult> UseConnectionAsyncFlex<TResult>(SyncOrAsync syncOrAsync, Func<IComposableOracleConnection, Task<TResult>> func) =>
                _pool.Value.UseConnectionAsyncFlex(syncOrAsync, func);
        }
    }
}
