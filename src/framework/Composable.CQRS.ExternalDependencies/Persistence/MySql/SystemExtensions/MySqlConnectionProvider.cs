using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.TransactionsCE;
using MySql.Data.MySqlClient;

namespace Composable.Persistence.MySql.SystemExtensions
{
    class MySqlConnectionProvider : IMySqlConnectionProvider
    {
        readonly OptimizedLazy<string> _connectionString;
        string GetConnectionString() => _connectionString.Value;

        public MySqlConnectionProvider(string connectionString) : this(() => connectionString) {}

        public MySqlConnectionProvider(Func<string> connectionString) => _connectionString = new OptimizedLazy<string>(connectionString);

        async Task<MySqlConnection> OpenConnectionAsync(AsyncMode syncOrAsync)
        {
            using var escalationForbidden = TransactionCE.NoTransactionEscalationScope("Opening connection");
            var connectionString = GetConnectionString();
            var connection = new MySqlConnection(connectionString);

            await syncOrAsync.Run(
                                  () => connection.Open(),
                                  async () => await connection.OpenAsync().NoMarshalling())
                             .NoMarshalling();

            return connection;
        }

        public TResult UseConnection<TResult>(Func<MySqlConnection, TResult> func)
        {
            using var connection = OpenConnectionAsync(AsyncMode.Sync).AwaiterResult();
            return func(connection);
        }

        public void UseConnection(Action<MySqlConnection> action) => UseConnection(action.AsFunc());

        public async Task<TResult> UseConnectionAsync<TResult>(Func<MySqlConnection, Task<TResult>> func)
        {
            await using var connection = await OpenConnectionAsync(AsyncMode.Async).NoMarshalling();
            return await func(connection).NoMarshalling();
        }

        public async Task UseConnectionAsync(Func<MySqlConnection, Task> action) => await UseConnectionAsync(action.AsFunc()).NoMarshalling();
    }
}
