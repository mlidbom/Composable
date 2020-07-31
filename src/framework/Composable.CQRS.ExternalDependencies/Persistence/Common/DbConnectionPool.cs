using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using System.Transactions;
using Composable.SystemCE.CollectionsCE.GenericCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using Composable.SystemCE.TransactionsCE;

namespace Composable.Persistence.Common
{
    abstract class DbConnectionPool<TConnection, TCommand> : IDbConnectionPool<TConnection, TCommand>
        where TConnection : IPoolableConnection, IComposableDbConnection<TCommand>
        where TCommand : DbCommand
    {
        static readonly OptimizedThreadShared<Dictionary<string, DbConnectionPool<TConnection, TCommand>>> Pools =
            new OptimizedThreadShared<Dictionary<string, DbConnectionPool<TConnection, TCommand>>>(new Dictionary<string, DbConnectionPool<TConnection, TCommand>>());

        internal static IDbConnectionPool<TConnection, TCommand> ForConnectionString(string connectionString, PoolableConnectionFlags flags, Func<string, TConnection> createConnection) =>
            Pools.WithExclusiveAccess(func: pools => pools.GetOrAdd(connectionString, constructor: () => Create(connectionString, flags, createConnection)));

        readonly string _connectionString;
        readonly Func<string, TConnection> _createConnection;
        DbConnectionPool(string connectionString, Func<string, TConnection> createConnection)
        {
            _connectionString = connectionString;
            _createConnection = createConnection;
        }

        static DbConnectionPool<TConnection, TCommand> Create(string connectionString, PoolableConnectionFlags flags, Func<string, TConnection> createConnection)
        {
            if(flags.HasFlag(PoolableConnectionFlags.MustUseSameConnectionThroughoutATransaction)) return new TransactionAffinityDbConnectionPool(connectionString, createConnection);

            return new DefaultDbConnectionPool(connectionString, createConnection);
        }

        public abstract Task<TResult> UseConnectionAsyncFlex<TResult>(AsyncMode syncOrAsync, Func<TConnection, Task<TResult>> func);

        async Task<TConnection> OpenConnectionAsyncFlex(AsyncMode syncOrAsync)
        {
            using var escalationForbidden = TransactionCE.NoTransactionEscalationScope($"Opening {typeof(TConnection)}");
            var connection = _createConnection(_connectionString);
            await syncOrAsync.Run(connection.OpenAsyncFlex).NoMarshalling();
            return connection;
        }

        class DefaultDbConnectionPool : DbConnectionPool<TConnection, TCommand>
        {
            public DefaultDbConnectionPool(string connectionString, Func<string, TConnection> createConnection) : base(connectionString, createConnection) {}
            public override async Task<TResult> UseConnectionAsyncFlex<TResult>(AsyncMode syncOrAsync, Func<TConnection, Task<TResult>> func)
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

                Task<TConnection> getConnectionTask;
                if(transactionLocalIdentifier == null)
                    getConnectionTask = syncOrAsync.Run(OpenConnectionAsyncFlex);
                else
                    //TConnection requires that the same connection is used throughout a transaction
                    getConnectionTask = _transactionConnections.WithExclusiveAccess(
                        func: transactionConnections => transactionConnections.GetOrAdd(
                            Transaction.Current!.TransactionInformation.LocalIdentifier,
                            constructor: () =>
                            {
                                var createConnectionTask = syncOrAsync.Run(OpenConnectionAsyncFlex);
                                Transaction.Current.OnCompleted(action: () => _transactionConnections.WithExclusiveAccess(func: transactionConnectionsAfterTransaction =>
                                {
                                    createConnectionTask.Result.Dispose();
                                    transactionConnectionsAfterTransaction.Remove(transactionLocalIdentifier);
                                }));
                                return createConnectionTask;
                            }));

                var connection = await getConnectionTask.NoMarshalling();
                if(transactionLocalIdentifier != null)
                    return await func(connection).NoMarshalling();
                else
                    await using(connection)
                    {
                        return await func(connection).NoMarshalling();
                    }
            }
        }
    }
}
