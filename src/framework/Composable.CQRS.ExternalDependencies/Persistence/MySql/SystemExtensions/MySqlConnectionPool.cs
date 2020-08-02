using System;
using System.Threading.Tasks;
using Composable.Persistence.Common.AdoCE;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE;
using MySql.Data.MySqlClient;

namespace Composable.Persistence.MySql.SystemExtensions
{
    interface IMySqlConnectionPool : IDbConnectionPool<IComposableMySqlConnection, MySqlCommand>
    {
        public static IMySqlConnectionPool CreateInstance(string connectionString) => CreateInstance(() => connectionString);
        public static MySqlConnectionPool CreateInstance(Func<string> getConnectionString) => new MySqlConnectionPool(getConnectionString);

        class MySqlConnectionPool : IMySqlConnectionPool
        {
            readonly OptimizedLazy<IDbConnectionPool<IComposableMySqlConnection, MySqlCommand>> _pool;

            public MySqlConnectionPool(Func<string> getConnectionString)
            {
                _pool = new OptimizedLazy<IDbConnectionPool<IComposableMySqlConnection, MySqlCommand>>(
                    () =>
                    {
                        var connectionString = getConnectionString();
                        return DbConnectionPool<IComposableMySqlConnection, MySqlCommand>.ForConnectionString(
                            connectionString,
                            PoolableConnectionFlags.Defaults,
                            IComposableMySqlConnection.Create);
                    });
            }

            public Task<TResult> UseConnectionAsyncFlex<TResult>(SyncOrAsync syncOrAsync, Func<IComposableMySqlConnection, Task<TResult>> func)
                => _pool.Value.UseConnectionAsyncFlex(syncOrAsync, func);
        }
    }
}
