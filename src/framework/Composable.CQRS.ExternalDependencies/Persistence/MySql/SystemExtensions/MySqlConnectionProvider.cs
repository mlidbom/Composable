using System;
using System.Threading.Tasks;
using Composable.Persistence.Common;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE;
using MySql.Data.MySqlClient;

namespace Composable.Persistence.MySql.SystemExtensions
{
    class MySqlConnectionProvider : IMySqlConnectionProvider
    {
        readonly OptimizedLazy<IDbConnectionPool<ComposableMySqlConnection, MySqlCommand>> _pool;
        IDbConnectionPool<ComposableMySqlConnection, MySqlCommand> Pool => _pool.Value;

        public MySqlConnectionProvider(string connectionString) : this(() => connectionString) {}

        public MySqlConnectionProvider(Func<string> getConnectionString)
        {
            _pool = new OptimizedLazy<IDbConnectionPool<ComposableMySqlConnection, MySqlCommand>>(
                () =>
                {
                    var connectionString = getConnectionString();
                    return DbConnectionPool<ComposableMySqlConnection, MySqlCommand>.ForConnectionString(
                        connectionString,
                        PoolableConnectionFlags.Defaults,
                        ComposableMySqlConnection.Create);
                });
        }

        public TResult UseConnection<TResult>(Func<ComposableMySqlConnection, TResult> func) => Pool.UseConnection(func);

        public void UseConnection(Action<ComposableMySqlConnection> action) => Pool.UseConnection(action);

        public async Task UseConnectionAsync(Func<ComposableMySqlConnection, Task> action) => await Pool.UseConnectionAsync(action).NoMarshalling();

        public async Task<TResult> UseConnectionAsync<TResult>(Func<ComposableMySqlConnection, Task<TResult>> func) => await Pool.UseConnectionAsync(func).NoMarshalling();
    }
}
