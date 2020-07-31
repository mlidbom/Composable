using System;
using System.Threading.Tasks;
using Composable.Persistence.Common;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE;
using IBM.Data.DB2.Core;

namespace Composable.Persistence.DB2.SystemExtensions
{
    interface IDB2ConnectionPool : IDbConnectionPool<IComposableDB2Connection, DB2Command>
    {
        public static IDB2ConnectionPool CreateInstance(string connectionString) => CreateInstance(() => connectionString);
        public static IDB2ConnectionPool CreateInstance(Func<string> getConnectionString) => new DB2ConnectionPool(getConnectionString);

        class DB2ConnectionPool : IDB2ConnectionPool
        {
            readonly OptimizedLazy<IDbConnectionPool<IComposableDB2Connection, DB2Command>> _pool;

            public DB2ConnectionPool(Func<string> getConnectionString)
            {
                _pool = new OptimizedLazy<IDbConnectionPool<IComposableDB2Connection, DB2Command>>(
                    () =>
                    {
                        var connectionString = getConnectionString();
                        return DbConnectionPool<IComposableDB2Connection, DB2Command>.ForConnectionString(
                            connectionString,
                            PoolableConnectionFlags.MustUseSameConnectionThroughoutATransaction,
                            ComposableDB2Connection.Create);
                    });
            }
            public Task<TResult> UseConnectionAsyncFlex<TResult>(AsyncMode syncOrAsync, Func<IComposableDB2Connection, Task<TResult>> func) =>
                _pool.Value.UseConnectionAsyncFlex(syncOrAsync, func);
        }
    }
}
