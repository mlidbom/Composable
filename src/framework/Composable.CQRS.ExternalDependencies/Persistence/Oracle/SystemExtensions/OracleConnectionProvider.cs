using System;
using System.Threading.Tasks;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.TransactionsCE;
using Oracle.ManagedDataAccess.Client;

namespace Composable.Persistence.Oracle.SystemExtensions
{
    class OracleConnectionProvider : IOracleConnectionProvider
    {
        readonly OptimizedLazy<string> _connectionString;
        string GetConnectionString() => _connectionString.Value;

        public OracleConnectionProvider(string connectionString) : this(() => connectionString) {}

        public OracleConnectionProvider(Func<string> connectionString) => _connectionString = new OptimizedLazy<string>(connectionString);

        //Performance: Since Oracle connection pooling is slow we should do something about that here.
        async Task<OracleConnection> OpenConnectionAsync(AsyncMode syncOrAsync)
        {
            using var escalationForbidden = TransactionCE.NoTransactionEscalationScope("Opening connection");
            var connectionString = GetConnectionString();
            var connection = new OracleConnection(connectionString);

            await syncOrAsync.Run(
                                  () => connection.Open(),
                                  () => connection.OpenAsync())
                             .NoMarshalling();

            return connection;
        }

        public TResult UseConnection<TResult>(Func<OracleConnection, TResult> func)
        {
            using var connection = OpenConnectionAsync(AsyncMode.Sync).GetAwaiterResult();
            return func(connection);
        }

        public void UseConnection(Action<OracleConnection> action) => UseConnection(action.AsFunc());

        public async Task<TResult> UseConnectionAsync<TResult>(Func<OracleConnection, Task<TResult>> func)
        {
            await using var connection = await OpenConnectionAsync(AsyncMode.Async).NoMarshalling();
            return await func(connection).NoMarshalling();
        }

        public async Task UseConnectionAsync(Func<OracleConnection, Task> action) => await UseConnectionAsync(action.AsFunc()).NoMarshalling();
    }
}
