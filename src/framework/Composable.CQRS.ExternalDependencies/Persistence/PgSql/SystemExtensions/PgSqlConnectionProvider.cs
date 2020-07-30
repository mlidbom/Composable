using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;
using Composable.SystemCE;
using Composable.SystemCE.CollectionsCE.GenericCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using Composable.SystemCE.TransactionsCE;
using Npgsql;

namespace Composable.Persistence.PgSql.SystemExtensions
{
    class PgSqlConnectionProvider : INpgsqlConnectionProvider
    {
        readonly OptimizedLazy<string> _connectionString;
        string GetConnectionString() => _connectionString.Value;

        public PgSqlConnectionProvider(string connectionString) : this(() => connectionString) {}

        public PgSqlConnectionProvider(Func<string> connectionString) => _connectionString = new OptimizedLazy<string>(connectionString);

        readonly OptimizedThreadShared<Dictionary<string, Task<NpgsqlConnection>>> _transactionConnections = new OptimizedThreadShared<Dictionary<string, Task<NpgsqlConnection>>>(new Dictionary<string, Task<NpgsqlConnection>>());

        public TResult UseConnection<TResult>(Func<NpgsqlConnection, TResult> func) => UseConnectionAsync(AsyncMode.Sync, func.AsAsync()).GetAwaiterResult();

        public void UseConnection(Action<NpgsqlConnection> action) => UseConnectionAsync(AsyncMode.Sync, action.AsFunc().AsAsync()).GetAwaiterResult();

        public async Task UseConnectionAsync(Func<NpgsqlConnection, Task> action) => await UseConnectionAsync(AsyncMode.Async, action.AsFunc()).NoMarshalling();

        public async Task<TResult> UseConnectionAsync<TResult>(Func<NpgsqlConnection, Task<TResult>> func) => await UseConnectionAsync(AsyncMode.Async, func).NoMarshalling();

        async Task<TResult> UseConnectionAsync<TResult>(AsyncMode syncOrAsync, Func<NpgsqlConnection, Task<TResult>> func)
        {
            Task<NpgsqlConnection> getConnectionTask;
            var inTransaction = Transaction.Current != null;
            if(!inTransaction)
            {
                getConnectionTask = syncOrAsync.Run(OpenConnectionAsync);
            } else
            {
                //PostgreSql has two problems with opening multiple connection within the same transaction:
                //1: It causes the transaction to escalate to distributed.
                //2: The other connections are unable to read data inserted by the first connection causing all sorts of havoc.
                //Thus we must ensure that the same connection is used throughout a transaction.
                getConnectionTask = _transactionConnections.WithExclusiveAccess(@this => @this.GetOrAdd(Transaction.Current!.TransactionInformation.LocalIdentifier,
                                                                                                     () =>
                                                                                                     {
                                                                                                         var transactionId = Transaction.Current.TransactionInformation.LocalIdentifier;
                                                                                                         var createConnectionTask = syncOrAsync.Run(OpenConnectionAsync);
                                                                                                         Transaction.Current.OnCompleted(() => _transactionConnections.WithExclusiveAccess(me =>
                                                                                                         {
                                                                                                             createConnectionTask.Result.Dispose();
                                                                                                             me.Remove(transactionId);
                                                                                                         }));
                                                                                                         return createConnectionTask;
                                                                                                     }));
            }

            var connection = await getConnectionTask.NoMarshalling();
            if(inTransaction)
            {
                return await func(connection).NoMarshalling();
            } else
            {

                await using(connection)
                {
                    return await func(connection).NoMarshalling();
                }
            }
        }

        async Task<NpgsqlConnection> OpenConnectionAsync(AsyncMode syncOrAsync)
        {
            using var escalationForbidden = TransactionCE.NoTransactionEscalationScope("Opening connection");
            var connectionString = GetConnectionString();
            var connection = new NpgsqlConnection(connectionString);
            await syncOrAsync.Run(
                                  () => connection.Open(),
                                  () => connection.OpenAsync())
                             .NoMarshalling();
            return connection;
        }
    }
}
