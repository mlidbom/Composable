using System;
using System.Threading.Tasks;
using Composable.Persistence.Common;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE;

namespace Composable.Persistence.DB2.SystemExtensions
{
    class DB2ConnectionProvider : IDB2ConnectionProvider
    {
        readonly OptimizedLazy<DbConnectionPool<ComposableDB2Connection>> _pool;
        DbConnectionPool<ComposableDB2Connection> Pool => _pool.Value;

        public DB2ConnectionProvider(string connectionString) : this(() => connectionString) {}

        public DB2ConnectionProvider(Func<string> getConnectionString)
        {
            _pool = new OptimizedLazy<DbConnectionPool<ComposableDB2Connection>>(
                () =>
                {
                    var connectionString = getConnectionString();
                    return DbConnectionPool<ComposableDB2Connection>.ForConnectionString(
                        connectionString,
                        PoolableConnectionFlags.MustUseSameConnectionThroughoutATransaction,
                        ComposableDB2Connection.Create);
                });
        }

        public TResult UseConnection<TResult>(Func<ComposableDB2Connection, TResult> func) => Pool.UseConnection(func);

        public void UseConnection(Action<ComposableDB2Connection> action) => Pool.UseConnection(action);

        public async Task UseConnectionAsync(Func<ComposableDB2Connection, Task> action) => await Pool.UseConnectionAsync(action).NoMarshalling();

        public async Task<TResult> UseConnectionAsync<TResult>(Func<ComposableDB2Connection, Task<TResult>> func) => await Pool.UseConnectionAsync(func).NoMarshalling();
    }
}
