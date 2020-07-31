using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Composable.Persistence.Common;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE;

namespace Composable.Persistence.MsSql.SystemExtensions
{
    class MsSqlConnectionProvider : IMsSqlConnectionProvider
    {
        readonly OptimizedLazy<IDbConnectionPool<ComposableMsSqlConnection, SqlCommand>> _pool;
        IDbConnectionPool<ComposableMsSqlConnection, SqlCommand> Pool => _pool.Value;

        public MsSqlConnectionProvider(string connectionString) : this(() => connectionString) {}

        public MsSqlConnectionProvider(Func<string> getConnectionString)
        {
            _pool = new OptimizedLazy<IDbConnectionPool<ComposableMsSqlConnection, SqlCommand>>(
                () =>
                {
                    var connectionString = getConnectionString();
                    return DbConnectionPool<ComposableMsSqlConnection, SqlCommand>.ForConnectionString(
                        connectionString,
                        PoolableConnectionFlags.Defaults,
                        ComposableMsSqlConnection.Create);
                });
        }

        public TResult UseConnection<TResult>(Func<ComposableMsSqlConnection, TResult> func) => Pool.UseConnection(func);

        public void UseConnection(Action<ComposableMsSqlConnection> action) => Pool.UseConnection(action);

        public async Task UseConnectionAsync(Func<ComposableMsSqlConnection, Task> action) => await Pool.UseConnectionAsync(action).NoMarshalling();

        public async Task<TResult> UseConnectionAsync<TResult>(Func<ComposableMsSqlConnection, Task<TResult>> func) => await Pool.UseConnectionAsync(func).NoMarshalling();
    }
}
