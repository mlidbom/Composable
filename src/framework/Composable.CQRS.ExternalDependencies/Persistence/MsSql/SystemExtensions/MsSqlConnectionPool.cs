using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Composable.Persistence.Common.AdoCE;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE;

namespace Composable.Persistence.MsSql.SystemExtensions
{
    interface IMsSqlConnectionPool : IDbConnectionPool<IComposableMsSqlConnection, SqlCommand>
    {
        public static IMsSqlConnectionPool CreateInstance(string connectionString) => CreateInstance(() => connectionString);
        public static MsSqlConnectionPool CreateInstance(Func<string> getConnectionString) => new MsSqlConnectionPool(getConnectionString);

        class MsSqlConnectionPool : IMsSqlConnectionPool
        {
            readonly OptimizedLazy<IDbConnectionPool<IComposableMsSqlConnection, SqlCommand>> _pool;
            IDbConnectionPool<IComposableMsSqlConnection, SqlCommand> Pool => _pool.Value;

            public MsSqlConnectionPool(Func<string> getConnectionString)
            {
                _pool = new OptimizedLazy<IDbConnectionPool<IComposableMsSqlConnection, SqlCommand>>(
                    () =>
                    {
                        var connectionString = getConnectionString();
                        return DbConnectionManager<IComposableMsSqlConnection, SqlCommand>.ForConnectionString(
                            connectionString,
                            PoolableConnectionFlags.Defaults,
                            IComposableMsSqlConnection.Create);
                    });
            }

            public Task<TResult> UseConnectionAsyncFlex<TResult>(SyncOrAsync syncOrAsync, Func<IComposableMsSqlConnection, Task<TResult>> func) => Pool.UseConnectionAsyncFlex(syncOrAsync, func);
        }
    }
}
