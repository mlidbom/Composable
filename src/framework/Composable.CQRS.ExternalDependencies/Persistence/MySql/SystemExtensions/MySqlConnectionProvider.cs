using System;
using System.Threading.Tasks;
using Composable.Persistence.Common;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE;
using MySql.Data.MySqlClient;

namespace Composable.Persistence.MySql.SystemExtensions
{
    interface IMySqlConnectionProvider : IDbConnectionPool<IComposableMySqlConnection, MySqlCommand>
    {
    }

    class MySqlConnectionProvider : IMySqlConnectionProvider
    {
        public static IMySqlConnectionProvider CreateInstance(string connectionString) => new MySqlConnectionProvider(connectionString);
        readonly OptimizedLazy<IDbConnectionPool<IComposableMySqlConnection, MySqlCommand>> _pool;
        IDbConnectionPool<IComposableMySqlConnection, MySqlCommand> Pool => _pool.Value;

        MySqlConnectionProvider(string connectionString) : this(() => connectionString) {}

        public MySqlConnectionProvider(Func<string> getConnectionString)
        {
            _pool = new OptimizedLazy<IDbConnectionPool<IComposableMySqlConnection, MySqlCommand>>(
                () =>
                {
                    var connectionString = getConnectionString();
                    return DbConnectionPool<IComposableMySqlConnection, MySqlCommand>.ForConnectionString(
                        connectionString,
                        PoolableConnectionFlags.Defaults,
                        ComposableMySqlConnection.Create);
                });
        }

        public Task<TResult> UseConnectionAsyncFlex<TResult>(AsyncMode syncOrAsync, Func<IComposableMySqlConnection, Task<TResult>> func)
            => _pool.Value.UseConnectionAsyncFlex(syncOrAsync, func);
    }
}
