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

        public ComposableDB2ConnectionProvider(string connectionString) : this(() => connectionString)
        {}

        public ComposableDB2ConnectionProvider(Func<string> connectionString) => _connectionString = new OptimizedLazy<string>(connectionString);

        ComposableDB2Connection OpenConnection()
        {
            using var escalationForbidden = TransactionCE.NoTransactionEscalationScope("Opening connection");
            var connectionString = GetConnectionString();
            var connection = new ComposableDB2Connection(connectionString);
            connection.Open();
            return connection;
        }

        readonly OptimizedThreadShared<Dictionary<string, ComposableDB2Connection>> _transactionConnections = new OptimizedThreadShared<Dictionary<string, ComposableDB2Connection>>(new Dictionary<string, ComposableDB2Connection>());
        public TResult UseConnection<TResult>(Func<ComposableDB2Connection, TResult> func)
        {
            if(Transaction.Current == null)
            {
                using var connection = OpenConnection();
                return func(connection);
            } else
            {
                //PostgreSql has two problems with opening multiple connection within the same transaction: 1: It causes the transaction to escalate to distributed. 2: The other connections are unable to read data inserted by the first connection causing all sorts of havoc.
                var connection = _transactionConnections.WithExclusiveAccess(@this => @this.GetOrAdd(Transaction.Current.TransactionInformation.LocalIdentifier,
                                                                                               () =>
                                                                                               {
                                                                                                   var transactionId = Transaction.Current.TransactionInformation.LocalIdentifier;
                                                                                                   var createdConnection = OpenConnection();
                                                                                                   Transaction.Current.OnCompleted(() => _transactionConnections.WithExclusiveAccess(me =>
                                                                                                   {
                                                                                                       createdConnection.Dispose();
                                                                                                       me.Remove(transactionId);
                                                                                                   }));
                                                                                                   return createdConnection;
                                                                                               }));
                return func(connection);
            }
        }

        public void UseConnection(Action<ComposableDB2Connection> action) => UseConnection(connection =>
        {
            action(connection);
            return 1;
        });

        //Urgent: All variations, in all persistence layers, on these async methods should use OpenAsync method on the connection.
        public async Task<TResult> UseConnectionAsync<TResult>(Func<ComposableDB2Connection, Task<TResult>> func)
        {
            await using var connection = OpenConnection();
            return await func(connection).NoMarshalling();
        }


        public async Task UseConnectionAsync(Func<ComposableDB2Connection, Task> action)
        {
            await using var connection = OpenConnection();
            await action(connection).NoMarshalling();
        }
    }
}
