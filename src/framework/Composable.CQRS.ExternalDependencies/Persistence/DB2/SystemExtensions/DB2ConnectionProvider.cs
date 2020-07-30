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
    class ComposableDB2ConnectionProvider : IComposableDB2ConnectionProvider
    {
        readonly OptimizedLazy<string> _connectionString;
        string GetConnectionString() => _connectionString.Value;

        public ComposableDB2ConnectionProvider(string connectionString) : this(() => connectionString) {}

        public ComposableDB2ConnectionProvider(Func<string> connectionString) => _connectionString = new OptimizedLazy<string>(connectionString);

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

        readonly OptimizedThreadShared<Dictionary<string, ComposableDB2Connection>> _transactionConnections = new OptimizedThreadShared<Dictionary<string, ComposableDB2Connection>>(new Dictionary<string, ComposableDB2Connection>());
        public TResult UseConnection<TResult>(Func<ComposableDB2Connection, TResult> func)
        {
            ComposableDB2Connection connection;
            var inTransaction = Transaction.Current != null;
            if(!inTransaction)
            {
                connection = OpenConnectionAsync(AsyncMode.Sync).GetAwaiterResult();
            } else
            {
                //Since we have failed to get DB2Connection to enlist in local transactions in any way we create DB2Transactions in our wrapper class which enlists. Because of that we must ensure that the same instance is used throughout a transaction.
                connection = _transactionConnections.WithExclusiveAccess(@this => @this.GetOrAdd(Transaction.Current!.TransactionInformation.LocalIdentifier,
                                                                                                     () =>
                                                                                                     {
                                                                                                         var transactionId = Transaction.Current.TransactionInformation.LocalIdentifier;
                                                                                                         var createdConnection = OpenConnectionAsync(AsyncMode.Sync).GetAwaiterResult();
                                                                                                         Transaction.Current.OnCompleted(() => _transactionConnections.WithExclusiveAccess(me =>
                                                                                                         {
                                                                                                             createdConnection.Dispose();
                                                                                                             me.Remove(transactionId);
                                                                                                         }));
                                                                                                         return createdConnection;
                                                                                                     }));
            }

            if(inTransaction)
            {
                return func(connection);
            } else
            {

                using(connection)
                {
                    return func(connection);
                }
            }
        }

        public void UseConnection(Action<ComposableDB2Connection> action) => UseConnection(action.AsFunc());


        public async Task<TResult> UseConnectionAsync<TResult>(Func<ComposableDB2Connection, Task<TResult>> func) => await UseConnectionAsync(AsyncMode.Async, func).NoMarshalling();

        public async Task UseConnectionAsync(Func<ComposableDB2Connection, Task> action) => await UseConnectionAsync(AsyncMode.Async, action.AsFunc()).NoMarshalling();

        async Task<TResult> UseConnectionAsync<TResult>(AsyncMode syncOrAsync, Func<ComposableDB2Connection, Task<TResult>> func)
        {
            await using var connection = await syncOrAsync.Run(OpenConnectionAsync).NoMarshalling();
            return await func(connection).NoMarshalling();
        }
    }
}
