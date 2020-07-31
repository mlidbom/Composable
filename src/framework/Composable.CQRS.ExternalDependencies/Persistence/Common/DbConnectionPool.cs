using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Transactions;
using Composable.Persistence.DB2;
using Composable.SystemCE.CollectionsCE.GenericCE;
using Composable.SystemCE.ReflectionCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using Composable.SystemCE.TransactionsCE;

namespace Composable.Persistence.Common
{
    [Flags] enum PoolableConnectionFlags
    {
        Defaults = 0,
        MustUseSameConnectionThroughoutATransaction = 1
    }

    interface IComposableDbConnection
    {
        IDbCommand CreateCommand();
        //Urgent: Remove this as soon as all persistence layers implement this interface so we can migrate to using CreateCommand.
        IDbConnection Connection { get; }
    }

    interface IComposableDbConnection<out TCommand> : IComposableDbConnection where TCommand : IDbCommand
    {
        new TCommand CreateCommand();
    }

    interface IPoolableConnection : IDisposable, IAsyncDisposable
    {
        Task OpenAsyncFlex(AsyncMode syncOrAsync);
    }

    abstract class DbConnectionPool<TConnection> where TConnection : IPoolableConnection
    {
        static readonly OptimizedThreadShared<Dictionary<string, DbConnectionPool<TConnection>>> Pools =
            new OptimizedThreadShared<Dictionary<string, DbConnectionPool<TConnection>>>(new Dictionary<string, DbConnectionPool<TConnection>>());

        internal static DbConnectionPool<TConnection> ForConnectionString(string connectionString, PoolableConnectionFlags flags, Func<string, TConnection> createConnection) =>
            Pools.WithExclusiveAccess(pools => pools.GetOrAdd(connectionString, () => Create(connectionString, flags, createConnection)));


        readonly string _connectionString;
        readonly Func<string, TConnection> _createConnection;
        DbConnectionPool(string connectionString, Func<string, TConnection> createConnection)
        {
            _connectionString = connectionString;
            _createConnection = createConnection;
        }

        static DbConnectionPool<TConnection> Create(string connectionString, PoolableConnectionFlags flags, Func<string, TConnection> createConnection)
        {
            if(flags.HasFlag(PoolableConnectionFlags.MustUseSameConnectionThroughoutATransaction))
            {
                return new TransactionAffinityDbConnectionPool(connectionString, createConnection);
            }
            return new DefaultDbConnectionPool(connectionString, createConnection);
        }

        public TResult UseConnection<TResult>(Func<TConnection, TResult> func) =>
            UseConnectionAsync(AsyncMode.Sync, func.AsAsync()).AwaiterResult();

        public void UseConnection(Action<TConnection> action) =>
            UseConnectionAsync(AsyncMode.Sync, action.AsFunc().AsAsync()).AwaiterResult();

        public async Task UseConnectionAsync(Func<TConnection, Task> action) =>
            await UseConnectionAsync(AsyncMode.Async, action.AsFunc()).NoMarshalling();

        public async Task<TResult> UseConnectionAsync<TResult>(Func<TConnection, Task<TResult>> func) =>
            await UseConnectionAsync(AsyncMode.Async, func).NoMarshalling();

        protected abstract Task<TResult> UseConnectionAsync<TResult>(AsyncMode syncOrAsync, Func<TConnection, Task<TResult>> func);

        async Task<TConnection> OpenConnectionAsync(AsyncMode syncOrAsync)
        {
            using var escalationForbidden = TransactionCE.NoTransactionEscalationScope($"Opening {typeof(TConnection)}");
            var connection = _createConnection(_connectionString);
            await syncOrAsync.Run(connection.OpenAsyncFlex).NoMarshalling();
            return connection;
        }

        class DefaultDbConnectionPool : DbConnectionPool<TConnection>
        {
            public DefaultDbConnectionPool(string connectionString, Func<string, TConnection> createConnection) : base(connectionString, createConnection) {}
            protected override Task<TResult> UseConnectionAsync<TResult>(AsyncMode syncOrAsync, Func<TConnection, Task<TResult>> func) => throw new NotImplementedException();
        }

        class TransactionAffinityDbConnectionPool : DbConnectionPool<TConnection>
        {
            readonly OptimizedThreadShared<Dictionary<string, Task<TConnection>>> _transactionConnections =
                new OptimizedThreadShared<Dictionary<string, Task<TConnection>>>(new Dictionary<string, Task<TConnection>>());

            public TransactionAffinityDbConnectionPool(string connectionString, Func<string, TConnection> createConnection):base(connectionString, createConnection) {  }

            protected override async Task<TResult> UseConnectionAsync<TResult>(AsyncMode syncOrAsync, Func<TConnection, Task<TResult>> func)
            {
                Task<TConnection> getConnectionTask;
                var inTransaction = Transaction.Current != null;
                if(!inTransaction)
                {
                    getConnectionTask = syncOrAsync.Run(OpenConnectionAsync);
                } else
                {
                    //TConnection requires that the same connection is used throughout a transaction
                    getConnectionTask = _transactionConnections.WithExclusiveAccess(transactionConnections => transactionConnections.GetOrAdd(Transaction.Current!.TransactionInformation.LocalIdentifier,
                                                                                                            () =>
                                                                                                            {
                                                                                                                var transactionLocalIdentifier = Transaction.Current.TransactionInformation.LocalIdentifier;
                                                                                                                var createConnectionTask = syncOrAsync.Run(OpenConnectionAsync);
                                                                                                                Transaction.Current.OnCompleted(() => _transactionConnections.WithExclusiveAccess(transactionConnectionsAfterTransaction =>
                                                                                                                {
                                                                                                                    createConnectionTask.Result.Dispose();
                                                                                                                    transactionConnectionsAfterTransaction.Remove(transactionLocalIdentifier);
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
        }
    }
}
