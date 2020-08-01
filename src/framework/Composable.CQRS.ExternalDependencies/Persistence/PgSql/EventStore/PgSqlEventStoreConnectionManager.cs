using System;
using System.Transactions;
using Composable.Persistence.PgSql.SystemExtensions;
using JetBrains.Annotations;
using Npgsql;

namespace Composable.Persistence.PgSql.EventStore
{
    class PgSqlEventStoreConnectionManager
    {
        readonly IPgSqlConnectionPool _connectionPool;
        public PgSqlEventStoreConnectionManager(IPgSqlConnectionPool sqlConnectionPool) => _connectionPool = sqlConnectionPool;

        public void UseConnection([InstantHandle] Action<IComposableNpgsqlConnection> action)
        {
            AssertTransactionPolicy(false);
            _connectionPool.UseConnection(action);
        }

        public void UseCommand([InstantHandle]Action<NpgsqlCommand> action) => UseCommand(false, action);
        public void UseCommand(bool suppressTransactionWarning, [InstantHandle] Action<NpgsqlCommand> action)
        {
            AssertTransactionPolicy(suppressTransactionWarning);
            _connectionPool.UseCommand(action);
        }

        public TResult UseCommand<TResult>([InstantHandle]Func<NpgsqlCommand, TResult> action) => UseCommand(false, action);
        public TResult UseCommand<TResult>(bool suppressTransactionWarning, [InstantHandle] Func<NpgsqlCommand, TResult> action)
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