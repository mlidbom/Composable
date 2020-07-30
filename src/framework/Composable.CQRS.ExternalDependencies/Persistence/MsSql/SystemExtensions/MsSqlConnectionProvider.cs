using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.TransactionsCE;

namespace Composable.Persistence.MsSql.SystemExtensions
{
    class MsSqlConnectionProvider : IMsSqlConnectionProvider
    {
        readonly OptimizedLazy<string> _connectionString;
        string GetConnectionString() => _connectionString.Value;

        public MsSqlConnectionProvider(string connectionString) : this(() => connectionString) {}

        public MsSqlConnectionProvider(Func<string> connectionString) => _connectionString = new OptimizedLazy<string>(connectionString);

        async Task<SqlConnection> OpenConnectionAsync(AsyncMode mode)
        {
            using var escalationForbidden = TransactionCE.NoTransactionEscalationScope("Opening connection");
            var connectionString = GetConnectionString();
            var connection = new SqlConnection(connectionString);

            if(mode == AsyncMode.Async)
                await connection.OpenAsync().NoMarshalling();
            else
                connection.Open();

            return connection;
        }

        public TResult UseConnection<TResult>(Func<SqlConnection, TResult> func)
        {
            using var connection = OpenConnectionAsync(AsyncMode.Sync).GetAwaiter().GetResult();
            return func(connection);
        }

        public void UseConnection(Action<SqlConnection> action) => UseConnection(connection =>
        {
            action(connection);
            return 1;
        });

        public async Task<TResult> UseConnectionAsync<TResult>(Func<SqlConnection, Task<TResult>> func)
        {
            await using var connection = await OpenConnectionAsync(AsyncMode.Async).NoMarshalling();
            return await func(connection).NoMarshalling();
        }

        public async Task UseConnectionAsync(Func<SqlConnection, Task> action)
        {
            await using var connection = await OpenConnectionAsync(AsyncMode.Async).NoMarshalling();
            await action(connection).NoMarshalling();
        }
    }
}
