using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;
using Composable.SystemCE;
using Composable.SystemCE.CollectionsCE.GenericCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using Composable.SystemCE.TransactionsCE;

namespace Composable.Persistence.DB2.SystemExtensions
{
    class DB2ConnectionProvider : IDB2ConnectionProvider
    {
        readonly OptimizedLazy<string> _connectionString;
        string GetConnectionString() => _connectionString.Value;

        public DB2ConnectionProvider(string connectionString) : this(() => connectionString) {}

        public DB2ConnectionProvider(Func<string> connectionString) => _connectionString = new OptimizedLazy<string>(connectionString);

        readonly OptimizedThreadShared<Dictionary<string, Task<ComposableDB2Connection>>> _transactionConnections = new OptimizedThreadShared<Dictionary<string, Task<ComposableDB2Connection>>>(new Dictionary<string, Task<ComposableDB2Connection>>());

        public TResult UseConnection<TResult>(Func<ComposableDB2Connection, TResult> func) => UseConnectionAsync(AsyncMode.Sync, func.AsAsync()).AwaiterResult();

        public void UseConnection(Action<ComposableDB2Connection> action) => UseConnectionAsync(AsyncMode.Sync, action.AsFunc().AsAsync()).AwaiterResult();

        public async Task UseConnectionAsync(Func<ComposableDB2Connection, Task> action) => await UseConnectionAsync(AsyncMode.Async, action.AsFunc()).NoMarshalling();

        public async Task<TResult> UseConnectionAsync<TResult>(Func<ComposableDB2Connection, Task<TResult>> func) => await UseConnectionAsync(AsyncMode.Async, func).NoMarshalling();

        async Task<TResult> UseConnectionAsync<TResult>(AsyncMode syncOrAsync, Func<ComposableDB2Connection, Task<TResult>> func)
        {
            Task<ComposableDB2Connection> getConnectionTask;
            var inTransaction = Transaction.Current != null;
            if(!inTransaction)
            {
                getConnectionTask = syncOrAsync.Run(OpenConnectionAsync);
            } else
            {
                //Since we have failed to get DB2Connection to enlist in local transactions in any way we create DB2Transactions in our wrapper class which enlists. Because of that we must ensure that the same instance is used throughout a transaction.
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

        async Task<ComposableDB2Connection> OpenConnectionAsync(AsyncMode syncOrAsync)
        {
            using var escalationForbidden = TransactionCE.NoTransactionEscalationScope("Opening connection");
            var connectionString = GetConnectionString();
            var connection = new ComposableDB2Connection(connectionString);
            await syncOrAsync.Run(
                                  () => connection.Open(),
                                  () => connection.OpenAsync())
                             .NoMarshalling();
            return connection;
        }
    }
}
