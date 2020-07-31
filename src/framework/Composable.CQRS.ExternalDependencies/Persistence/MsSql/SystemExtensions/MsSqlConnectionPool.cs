using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Composable.Persistence.Common;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE;

namespace Composable.Persistence.MsSql.SystemExtensions
{
    interface IMsSqlConnectionPool : IDbConnectionPool<IComposableMsSqlConnection, SqlCommand>
    {
    }

    class MsSqlConnectionPool : IMsSqlConnectionPool
    {
        public static IMsSqlConnectionPool CreateInstance(string connectionString) => new MsSqlConnectionPool(connectionString);
        readonly OptimizedLazy<IDbConnectionPool<IComposableMsSqlConnection, SqlCommand>> _pool;
        IDbConnectionPool<IComposableMsSqlConnection, SqlCommand> Pool => _pool.Value;

        MsSqlConnectionPool(string connectionString) : this(() => connectionString) {}

        public MsSqlConnectionPool(Func<string> getConnectionString)
        {
            _pool = new OptimizedLazy<IDbConnectionPool<IComposableMsSqlConnection, SqlCommand>>(
                () =>
                {
                    var connectionString = getConnectionString();
                    return DbConnectionPool<IComposableMsSqlConnection, SqlCommand>.ForConnectionString(
                        connectionString,
                        PoolableConnectionFlags.Defaults,
                        ComposableMsSqlConnection.Create);
                });
        }

        public Task<TResult> UseConnectionAsyncFlex<TResult>(AsyncMode syncOrAsync, Func<IComposableMsSqlConnection, Task<TResult>> func) => Pool.UseConnectionAsyncFlex(syncOrAsync, func);
    }
}
