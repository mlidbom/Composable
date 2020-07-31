using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Composable.SystemCE.CollectionsCE.GenericCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using Composable.SystemCE.TransactionsCE;

namespace Composable.Persistence.Common
{
    abstract partial class DbConnectionPool<TConnection, TCommand>
        where TConnection : IPoolableConnection, IComposableDbConnection<TCommand>
        where TCommand : DbCommand
    {
        class DefaultDbConnectionPool : DbConnectionPool<TConnection, TCommand>, IDbConnectionPool<TConnection, TCommand>
        {
            readonly string _connectionString;
            readonly Func<string, TConnection> _createConnection;

            public DefaultDbConnectionPool(string connectionString, Func<string, TConnection> createConnection)
            {
                _connectionString = connectionString;
                _createConnection = createConnection;
            }

            static int _openings = 0;
            protected async Task<TConnection> OpenConnectionAsyncFlex(AsyncMode syncOrAsync)
            {
                using var escalationForbidden = TransactionCE.NoTransactionEscalationScope($"Opening {typeof(TConnection)}");
                var connection = _createConnection(_connectionString);

                //This is here so that we can reassure ourselves, via a profiler, time and time again that it is actually the
                //first opening of a connection that takes very long and thus trying our own pooling will thus not help.
                if(Interlocked.Increment(ref _openings) == 1)
                {
                    await syncOrAsync.Run(connection.OpenAsyncFlex).NoMarshalling();
                } else
                {
                    await syncOrAsync.Run(connection.OpenAsyncFlex).NoMarshalling();
                }
                return connection;
            }


            public virtual async Task<TResult> UseConnectionAsyncFlex<TResult>(AsyncMode syncOrAsync, Func<TConnection, Task<TResult>> func)
            {
                await using var connection = await syncOrAsync.Run(OpenConnectionAsyncFlex).NoMarshalling();
                return await func(connection).NoMarshalling();
            }
        }

        class TransactionAffinityDbConnectionPool : DefaultDbConnectionPool
        {
            readonly OptimizedThreadShared<Dictionary<string, Task<TConnection>>> _transactionConnections =
                new OptimizedThreadShared<Dictionary<string, Task<TConnection>>>(new Dictionary<string, Task<TConnection>>());

            public TransactionAffinityDbConnectionPool(string connectionString, Func<string, TConnection> createConnection) : base(connectionString, createConnection) {}

            public override async Task<TResult> UseConnectionAsyncFlex<TResult>(AsyncMode syncOrAsync, Func<TConnection, Task<TResult>> func)
            {
                var transactionLocalIdentifier = Transaction.Current?.TransactionInformation.LocalIdentifier;

                if(transactionLocalIdentifier == null)
                {
                    return await base.UseConnectionAsyncFlex(syncOrAsync, func).NoMarshalling();
                } else
                {
                    //TConnection requires that the same connection is used throughout a transaction
                    var getConnectionTask = _transactionConnections.WithExclusiveAccess(
                        func: transactionConnections => transactionConnections.GetOrAdd(
                            transactionLocalIdentifier,
                            constructor: () =>
                            {
                                var createConnectionTask = syncOrAsync.Run(OpenConnectionAsyncFlex);
                                Transaction.Current!.OnCompleted(action: () => _transactionConnections.WithExclusiveAccess(func: transactionConnectionsAfterTransaction =>
                                {
                                    createConnectionTask.Result.Dispose();
                                    transactionConnectionsAfterTransaction.Remove(transactionLocalIdentifier);
                                }));
                                return createConnectionTask;
                            }));

                    var connection = await getConnectionTask.NoMarshalling();
                    return await func(connection).NoMarshalling();
                }
            }
        }
    }
}
