using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IBM.Data.DB2.Core;
using System.Transactions;
using Composable.Logging;
using Composable.System;
using Composable.System.Collections.Collections;
using Composable.System.Diagnostics;
using Composable.System.Threading;
using Composable.System.Threading.ResourceAccess;
using Composable.System.Transactions;
using Composable.SystemExtensions.TransactionsCE;
using IsolationLevel = System.Data.IsolationLevel;

namespace Composable.Persistence.DB2.SystemExtensions
{
    class ComposableDB2ConnectionProvider : IComposableDB2ConnectionProvider
    {
        string ConnectionString { get; }
        public ComposableDB2ConnectionProvider(string connectionString) => ConnectionString = connectionString;

        ComposableDB2Connection OpenConnection()
        {
            var transactionInformationDistributedIdentifierBefore = Transaction.Current?.TransactionInformation.DistributedIdentifier;
            var connection = GetConnectionFromPool();

            if(transactionInformationDistributedIdentifierBefore != null && transactionInformationDistributedIdentifierBefore.Value == Guid.Empty)
            {
                if(Transaction.Current!.TransactionInformation.DistributedIdentifier != Guid.Empty)
                {
                    throw new Exception("Opening connection escalated transaction to distributed. For now this is disallowed");
                }
            }

            return connection;
        }

        //Urgent: Since the DB2 connection pooling is way slow we should do something about that here. Something like using Task to keep a pool of open connections on hand.
        ComposableDB2Connection GetConnectionFromPool()
        {
            var connection = new ComposableDB2Connection(ConnectionString);
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
