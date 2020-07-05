using System.Collections.Generic;
using MySql.Data.MySqlClient;
using System.Linq;
using Composable.Persistence.MySql.SystemExtensions;
using Composable.System;
using Composable.System.Linq;
using Composable.System.Transactions;

namespace Composable.Persistence.MySql.Testing.Databases
{
    static class DatabaseExtensions
    {
        internal static string Name(this MySqlDatabasePool.Database @this) => $"{MySqlDatabasePool.PoolDatabaseNamePrefix}{@this.Id:0000}";
        internal static string ConnectionString(this MySqlDatabasePool.Database @this, MySqlDatabasePool pool) => pool.ConnectionStringForDbNamed(@this.Name());
    }

    sealed partial class MySqlDatabasePool
    {
        static readonly int MaxDatabases = 30;
        static void CreateDatabase(string databaseName)
        {
            var createDatabaseCommand = $@"
DROP DATABASE IF EXISTS {databaseName};
CREATE DATABASE {databaseName};";

            //Urgent: Figure out MySql equivalents and if they need to be specified
//            if(!_databaseRootFolderOverride.IsNullEmptyOrWhiteSpace())
//            {
//                createDatabaseCommand += $@"
//ON      ( NAME = {databaseName}_data, FILENAME = '{_databaseRootFolderOverride}\{databaseName}.mdf')
//LOG ON  ( NAME = {databaseName}_log, FILENAME = '{_databaseRootFolderOverride}\{databaseName}.ldf');";
//            }

//            createDatabaseCommand += $@"
//ALTER DATABASE [{databaseName}] SET RECOVERY SIMPLE;
//ALTER DATABASE[{ databaseName}] SET READ_COMMITTED_SNAPSHOT ON";

           _masterConnectionProvider?.ExecuteNonQuery(createDatabaseCommand);

            //SafeConsole.WriteLine($"Created: {databaseName}");
        }

        void RebootPool() => _machineWideState?.Update(RebootPool);

        void RebootPool(SharedState machineWide) => TransactionScopeCe.SuppressAmbient(() =>
        {
            RebootedMasterConnections.Add(_masterConnectionString!);
            _log.Warning("Rebooting database pool");

            machineWide.Reset();
            _transientCache = new List<Database>();

            _log.Warning("Creating new databases");

            InitializePool(machineWide);
        });

        void InitializePool(SharedState machineWide)
        {
            1.Through(MaxDatabases).ForEach(_ => InsertDatabase(machineWide));
        }
    }
}