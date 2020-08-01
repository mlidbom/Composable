using System;
using System.Collections.Generic;
using System.Data.Common;
using Composable.SystemCE.CollectionsCE.GenericCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;

namespace Composable.Persistence.Common.AdoCE
{
    abstract partial class DbConnectionPool<TConnection, TCommand>
        where TConnection : IPoolableConnection, IComposableDbConnection<TCommand>
        where TCommand : DbCommand
    {
        static readonly OptimizedThreadShared<Dictionary<string, IDbConnectionPool<TConnection, TCommand>>> Pools =
            new OptimizedThreadShared<Dictionary<string, IDbConnectionPool<TConnection, TCommand>>>(new Dictionary<string, IDbConnectionPool<TConnection, TCommand>>());

        internal static IDbConnectionPool<TConnection, TCommand> ForConnectionString(string connectionString, PoolableConnectionFlags flags, Func<string, TConnection> createConnection) =>
            Pools.WithExclusiveAccess(func: pools => pools.GetOrAdd(connectionString, constructor: () => Create(connectionString, flags, createConnection)));

        static IDbConnectionPool<TConnection, TCommand> Create(string connectionString, PoolableConnectionFlags flags, Func<string, TConnection> createConnection) =>
            flags.HasFlag(PoolableConnectionFlags.MustUseSameConnectionThroughoutATransaction)
                ? new TransactionAffinityDbConnectionPool(connectionString, createConnection)
                : new DefaultDbConnectionPool(connectionString, createConnection);
    }
}
