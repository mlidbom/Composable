using System;
using System.Transactions;
using Composable.Persistence.DB2.SystemExtensions;
using JetBrains.Annotations;
using IBM.Data.DB2.Core;

namespace Composable.Persistence.DB2.EventStore
{
    class DB2EventStoreConnectionManager
    {
        readonly IDB2ConnectionProvider _connectionProvider;
        public DB2EventStoreConnectionManager(IDB2ConnectionProvider sqlConnectionProvider) => _connectionProvider = sqlConnectionProvider;

        public void UseConnection([InstantHandle] Action<DB2Connection> action)
        {
            AssertTransactionPolicy(false);
            _connectionProvider.UseConnection(action);
        }

        public void UseCommand([InstantHandle]Action<DB2Command> action) => UseCommand(false, action);
        public void UseCommand(bool suppressTransactionWarning, [InstantHandle] Action<DB2Command> action)
        {
            AssertTransactionPolicy(suppressTransactionWarning);
            _connectionProvider.UseCommand(action);
        }

        public TResult UseCommand<TResult>([InstantHandle]Func<DB2Command, TResult> action) => UseCommand(false, action);
        public TResult UseCommand<TResult>(bool suppressTransactionWarning, [InstantHandle] Func<DB2Command, TResult> action)
        {
            AssertTransactionPolicy(suppressTransactionWarning);
            return _connectionProvider.UseCommand(action);
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