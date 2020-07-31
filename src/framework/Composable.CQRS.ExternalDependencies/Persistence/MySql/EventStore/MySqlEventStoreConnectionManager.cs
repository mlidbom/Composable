using System;
using System.Transactions;
using Composable.Persistence.MySql.SystemExtensions;
using JetBrains.Annotations;
using MySql.Data.MySqlClient;

namespace Composable.Persistence.MySql.EventStore
{
    class MySqlEventStoreConnectionManager
    {
        readonly IMySqlConnectionProvider _connectionProvider;
        public MySqlEventStoreConnectionManager(IMySqlConnectionProvider sqlConnectionProvider) => _connectionProvider = sqlConnectionProvider;

        public void UseConnection([InstantHandle] Action<IComposableMySqlConnection> action)
        {
            AssertTransactionPolicy(false);
            _connectionProvider.UseConnection(action);
        }

        public void UseCommand([InstantHandle]Action<MySqlCommand> action) => UseCommand(false, action);
        public void UseCommand(bool suppressTransactionWarning, [InstantHandle] Action<MySqlCommand> action)
        {
            AssertTransactionPolicy(suppressTransactionWarning);
            _connectionProvider.UseCommand(action);
        }

        public TResult UseCommand<TResult>([InstantHandle]Func<MySqlCommand, TResult> action) => UseCommand(false, action);
        public TResult UseCommand<TResult>(bool suppressTransactionWarning, [InstantHandle] Func<MySqlCommand, TResult> action)
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