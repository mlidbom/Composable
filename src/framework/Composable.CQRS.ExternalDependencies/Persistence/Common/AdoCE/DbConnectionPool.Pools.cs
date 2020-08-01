using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Composable.SystemCE;
using Composable.SystemCE.CollectionsCE.GenericCE;
using Composable.SystemCE.DiagnosticsCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using Composable.SystemCE.TransactionsCE;
// ReSharper disable StaticMemberInGenericType

namespace Composable.Persistence.Common.AdoCE
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

            static TimingsStatisticsCollector _subsequentOpenings = StopwatchCE.CreateCollector("Non-First connection openings",
                                                                                                1.Milliseconds(),
                                                                                                2.Milliseconds(),
                                                                                                3.Milliseconds(),
                                                                                                4.Milliseconds(),
                                                                                                5.Milliseconds(),
                                                                                                10.Milliseconds(),
                                                                                                20.Milliseconds()
            );

            static int _openings = 0;
            protected async Task<TConnection> OpenConnectionAsyncFlex(AsyncMode syncOrAsync)
            {
                using var escalationForbidden = TransactionCE.NoTransactionEscalationScope($"Opening {typeof(TConnection)}");
                var connection = _createConnection(_connectionString);

                //This is here so that we can reassure ourselves, via a profiler, time and time again that it is actually the
                //first opening of a connection that takes very long and thus trying our own pooling will not help.
                if(Interlocked.Increment(ref _openings) == 1)
                {
                    await syncOrAsync.Run(connection.OpenAsyncFlex).NoMarshalling();//Currently 120 passing tests total of 60 seconds runtime, average per test 500ms.
                } else
                {
                    //Currently about 900 passing tests. Total of 13 seconds. Average per test of 12ms.
                    //Our own pooling will NOT beat 12ms for all the connection usage in a db based test.
                    //Remember, this includes cleaning databases, creating tables, inserting data, reading the data etc.
                    //This is in other words probably less than 1ms per connection usage that will make at least one
                    //roundtrip to the db server in any case, likely eclipsing 1ms.
                    await _subsequentOpenings.TimeAsyncFlex(syncOrAsync, connection.OpenAsyncFlex).NoMarshalling();
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
