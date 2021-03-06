using System;
using System.Transactions;
using Composable.Persistence.Oracle.SystemExtensions;
using JetBrains.Annotations;
using Oracle.ManagedDataAccess.Client;

namespace Composable.Persistence.Oracle.EventStore
{
    class OracleEventStoreConnectionManager
    {
        readonly IOracleConnectionPool _connectionPool;
        public OracleEventStoreConnectionManager(IOracleConnectionPool sqlConnectionPool) => _connectionPool = sqlConnectionPool;

        public void UseConnection([InstantHandle] Action<IComposableOracleConnection> action)
        {
            AssertTransactionPolicy(false);
            _connectionPool.UseConnection(action);
        }

        public void UseCommand([InstantHandle]Action<OracleCommand> action) => UseCommand(false, action);
        public void UseCommand(bool suppressTransactionWarning, [InstantHandle] Action<OracleCommand> action)
        {
            AssertTransactionPolicy(suppressTransactionWarning);
            _connectionPool.UseCommand(action);
        }

        public TResult UseCommand<TResult>([InstantHandle]Func<OracleCommand, TResult> action) => UseCommand(false, action);
        public TResult UseCommand<TResult>(bool suppressTransactionWarning, [InstantHandle] Func<OracleCommand, TResult> action)
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