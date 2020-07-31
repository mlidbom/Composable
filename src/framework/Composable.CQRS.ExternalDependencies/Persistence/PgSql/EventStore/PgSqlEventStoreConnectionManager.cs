using System;
using System.Transactions;
using Composable.Persistence.PgSql.SystemExtensions;
using JetBrains.Annotations;
using Npgsql;

namespace Composable.Persistence.PgSql.EventStore
{
    class PgSqlEventStoreConnectionManager
    {
        readonly INpgsqlConnectionProvider _connectionProvider;
        public PgSqlEventStoreConnectionManager(INpgsqlConnectionProvider sqlConnectionProvider) => _connectionProvider = sqlConnectionProvider;

        public void UseConnection([InstantHandle] Action<ComposableNpgsqlConnection> action)
        {
            AssertTransactionPolicy(false);
            _connectionProvider.UseConnection(action);
        }

        public void UseCommand([InstantHandle]Action<NpgsqlCommand> action) => UseCommand(false, action);
        public void UseCommand(bool suppressTransactionWarning, [InstantHandle] Action<NpgsqlCommand> action)
        {
            AssertTransactionPolicy(suppressTransactionWarning);
            _connectionProvider.UseCommand(action);
        }

        public TResult UseCommand<TResult>([InstantHandle]Func<NpgsqlCommand, TResult> action) => UseCommand(false, action);
        public TResult UseCommand<TResult>(bool suppressTransactionWarning, [InstantHandle] Func<NpgsqlCommand, TResult> action)
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