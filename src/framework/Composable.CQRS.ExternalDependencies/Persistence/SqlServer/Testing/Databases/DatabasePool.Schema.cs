using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Composable.Persistence.SqlServer.SystemExtensions;
using Composable.System;
using Composable.System.Linq;
using Composable.System.Transactions;

namespace Composable.Persistence.SqlServer.Testing.Databases
{
    static class DatabaseExtensions
    {
        internal static string Name(this DatabasePool.Database @this) => $"{DatabasePool.PoolDatabaseNamePrefix}{@this.Id:0000}";
        internal static string ConnectionString(this DatabasePool.Database @this, DatabasePool pool) => pool.ConnectionStringForDbNamed(@this.Name());
    }

    partial class DatabasePool
    {
        static void CreateDatabase(string databaseName)
        {
            var createDatabaseCommand = $@"CREATE DATABASE [{databaseName}]";
            if(!_databaseRootFolderOverride.IsNullEmptyOrWhiteSpace())
            {
                createDatabaseCommand += $@"
ON      ( NAME = {databaseName}_data, FILENAME = '{_databaseRootFolderOverride}\{databaseName}.mdf') 
LOG ON  ( NAME = {databaseName}_log, FILENAME = '{_databaseRootFolderOverride}\{databaseName}.ldf');";
            }

            createDatabaseCommand += $@"
ALTER DATABASE [{databaseName}] SET RECOVERY SIMPLE;
ALTER DATABASE[{ databaseName}] SET READ_COMMITTED_SNAPSHOT ON";

            _masterConnectionProvider!.ExecuteNonQuery(createDatabaseCommand);

            //SafeConsole.WriteLine($"Created: {databaseName}");
        }

        void RebootPool() => _machineWideState?.Update(RebootPool);

        void RebootPool(SharedState machineWide) => TransactionScopeCe.SuppressAmbient(() =>
        {
            RebootedMasterConnections.Add(_masterConnectionString!);
            _log.Warning("Rebooting database pool");

            machineWide.Reset();
            _transientCache = new List<Database>();

            var dbsToDrop = ListPoolDatabases();

            _log.Warning("Dropping databases");
            foreach(var db in dbsToDrop)
            {
                //Clear connection pool
                using(var connection = new SqlConnection(db.ConnectionString(this)))
                {
                    SqlConnection.ClearPool(connection);
                }

                DropDatabase(db);
            }

            _log.Warning("Creating new databases");

            InitializePool(machineWide);
        });

        void DropDatabase(Database db)
        {
            var dropCommand = $@"
alter database [{db.Name()}] set single_user with rollback immediate
drop database [{db.Name()}]";
            _log.Info(dropCommand);
            _masterConnectionProvider?.ExecuteNonQuery(dropCommand);
        }

        void InitializePool(SharedState machineWide)
        {
            1.Through(30).ForEach(_ => InsertDatabase(machineWide));
        }

        static IReadOnlyList<Database> ListPoolDatabases()
        {
            var databases = new List<string>();
            _masterConnectionProvider?.UseCommand(
                action: command =>
                        {
                            command.CommandText = "select name from sysdatabases";
                            using var reader = command.ExecuteReader();
                            while(reader.Read())
                            {
                                var dbName = reader.GetString(i: 0);
                                if(dbName.StartsWith(PoolDatabaseNamePrefix))
                                    databases.Add(dbName);
                            }
                        });

            return databases.Select(name => new Database(name))
                            .ToList();
        }
    }
}