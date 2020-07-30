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

        async Task<SqlConnection> OpenConnectionAsync(AsyncMode syncOrAsync)
        {
            using var escalationForbidden = TransactionCE.NoTransactionEscalationScope("Opening connection");
            var connectionString = GetConnectionString();
            var connection = new SqlConnection(connectionString);

            await syncOrAsync.Run(connection.Open, connection.OpenAsync).NoMarshalling();

            return connection;
        }

        public TResult UseConnection<TResult>(Func<SqlConnection, TResult> func)
        {
            using var connection = OpenConnectionAsync(AsyncMode.Sync).GetAwaiterResult();
            return func(connection);
        }

        public void UseConnection(Action<SqlConnection> action) => UseConnection(action.AsFunc());

        public async Task<TResult> UseConnectionAsync<TResult>(Func<SqlConnection, Task<TResult>> func)
        {
            await using var connection = await OpenConnectionAsync(AsyncMode.Async).NoMarshalling();
            return await func(connection).NoMarshalling();
        }

        public async Task UseConnectionAsync(Func<SqlConnection, Task> action) => await UseConnectionAsync(action.AsFunc()).NoMarshalling();
    }
}
