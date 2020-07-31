using System;
using System.Data.SqlClient;
using System.Transactions;
using Composable.Persistence.MsSql.SystemExtensions;
using JetBrains.Annotations;

namespace Composable.Persistence.MsSql.EventStore
{
    class MsSqlEventStoreConnectionManager
    {
        readonly IMsSqlConnectionPool _connectionPool;
        public MsSqlEventStoreConnectionManager(IMsSqlConnectionPool sqlConnectionPool) => _connectionPool = sqlConnectionPool;

        public void UseConnection([InstantHandle] Action<IComposableMsSqlConnection> action)
        {
            AssertTransactionPolicy(false);
            _connectionPool.UseConnection(action);
        }

        public void UseCommand([InstantHandle]Action<SqlCommand> action) => UseCommand(false, action);
        public void UseCommand(bool suppressTransactionWarning, [InstantHandle] Action<SqlCommand> action)
        {
            AssertTransactionPolicy(suppressTransactionWarning);
            _connectionPool.UseCommand(action);
        }

        public TResult UseCommand<TResult>([InstantHandle]Func<SqlCommand, TResult> action) => UseCommand(false, action);
        public TResult UseCommand<TResult>(bool suppressTransactionWarning, [InstantHandle] Func<SqlCommand, TResult> action)
        {
            AssertTransactionPolicy(suppressTransactionWarning);
            return _connectionPool.UseCommand(action);
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        static void AssertTransactionPolicy(bool suppressTransactionWarning)
        {
            if (!suppressTransactionWarning && Transaction.Current == null)
            {
                throw new Exception("You must use a transaction to make modifications to the event store.");
            }
        }
    }
}