using System;
using System.Data.SqlClient;
using System.Transactions;
using Composable.Persistence.SqlServer.SystemExtensions;
using JetBrains.Annotations;

namespace Composable.Persistence.SqlServer.EventStore
{
    class SqlServerEventStoreConnectionManager
    {
        readonly ISqlServerConnectionProvider _connectionProvider;
        public SqlServerEventStoreConnectionManager(ISqlServerConnectionProvider sqlConnectionProvider) => _connectionProvider = sqlConnectionProvider;

        public void UseConnection([InstantHandle] Action<SqlConnection> action)
        {
            AssertTransactionPolicy(false);
            _connectionProvider.UseConnection(action);
        }

        public void UseCommand([InstantHandle]Action<SqlCommand> action) => UseCommand(false, action);
        public void UseCommand(bool suppressTransactionWarning, [InstantHandle] Action<SqlCommand> action)
        {
            AssertTransactionPolicy(suppressTransactionWarning);
            _connectionProvider.UseCommand(action);
        }

        public TResult UseCommand<TResult>([InstantHandle]Func<SqlCommand, TResult> action) => UseCommand<TResult>(false, action);
        public TResult UseCommand<TResult>(bool suppressTransactionWarning, [InstantHandle] Func<SqlCommand, TResult> action)
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