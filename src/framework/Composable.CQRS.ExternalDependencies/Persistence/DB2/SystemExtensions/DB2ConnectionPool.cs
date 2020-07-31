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
    }

    class DB2ConnectionPool : IDB2ConnectionPool
    {
        public static IDB2ConnectionPool CreateInstance(string connectionString) => new DB2ConnectionPool(connectionString);

        readonly OptimizedLazy<IDbConnectionPool<IComposableDB2Connection, DB2Command>> _pool;
        IDbConnectionPool<IComposableDB2Connection, DB2Command> Pool => _pool.Value;

        DB2ConnectionPool(string connectionString) : this(() => connectionString) {}

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
        public Task<TResult> UseConnectionAsyncFlex<TResult>(AsyncMode syncOrAsync, Func<IComposableDB2Connection, Task<TResult>> func) => Pool.UseConnectionAsyncFlex(syncOrAsync, func);
    }
}
