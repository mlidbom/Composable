using System.Collections.Generic;
using Composable.System.Linq;
using Composable.System.Transactions;

namespace Composable.Testing.Databases
{
    static class DatabaseExtensions
    {
        internal static string Name(this DatabasePool.Database @this) => $"{DatabasePool.PoolDatabaseNamePrefix}{@this.Id:0000}";
        internal static string ConnectionString(this DatabasePool.Database @this, DatabasePool pool) => pool.ConnectionStringForDbNamed(@this.Name());
    }

    partial class DatabasePool
    {
        void RebootPool() => _machineWideState?.Update(RebootPool);

        void RebootPool(SharedState machineWide) => TransactionScopeCe.SuppressAmbient(() =>
        {
            _log.Warning("Rebooting database pool");

            machineWide.Reset();
            _transientCache = new List<Database>();

            var dbsToDrop = ListPoolDatabases();

            _log.Warning("Dropping databases");
            foreach(var db in dbsToDrop)
            {
                //Clear connection pool
                ResetConnectionPool(db);

                DropDatabase(db);
            }

            _log.Warning("Creating new databases");

            InitializePool(machineWide);
        });

        void InitializePool(SharedState machineWide)
        {
            1.Through(30).ForEach(_ => InsertDatabase(machineWide));
        }

        protected abstract void CreateDatabase(string databaseName);

        protected abstract void ResetConnectionPool(Database db);

        protected abstract void DropDatabase(Database db);

        protected abstract IReadOnlyList<Database> ListPoolDatabases();
    }
}