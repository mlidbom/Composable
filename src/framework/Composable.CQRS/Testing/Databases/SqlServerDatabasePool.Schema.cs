using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Composable.System;
using Composable.System.Data.SqlClient;
using Composable.System.Linq;
using Composable.System.Transactions;

namespace Composable.Testing.Databases
{
    static class DatabaseExtensions
    {
        internal static string Name(this SqlServerDatabasePool.Database @this) => $"{SqlServerDatabasePool.PoolDatabaseNamePrefix}{@this.Id:0000}";
        internal static string ConnectionString(this SqlServerDatabasePool.Database @this, SqlServerDatabasePool pool) => pool.ConnectionStringForDbNamed(@this.Name());
    }

    sealed partial class SqlServerDatabasePool
    {
        void CreateDatabase(string databaseName)
        {
            var createDatabaseCommand = $@"CREATE DATABASE [{databaseName}]";
            if(!DatabaseRootFolderOverride.IsNullOrWhiteSpace())
            {
                createDatabaseCommand += $@"
ON      ( NAME = {databaseName}_data, FILENAME = '{DatabaseRootFolderOverride}\{databaseName}.mdf') 
LOG ON  ( NAME = {databaseName}_log, FILENAME = '{DatabaseRootFolderOverride}\{databaseName}.ldf');";
            }

            createDatabaseCommand += $@"
ALTER DATABASE [{databaseName}] SET RECOVERY SIMPLE;
ALTER DATABASE[{ databaseName}] SET READ_COMMITTED_SNAPSHOT ON";

            _masterConnection.ExecuteNonQuery(createDatabaseCommand);

            //SafeConsole.WriteLine($"Created: {databaseName}");
        }

        void RebootPool(SharedState machineWide) => TransactionScopeCe.SuppressAmbient(() =>
        {
            RebootedMasterConnections.Add(_masterConnectionString);
            _log.Warning("Rebooting database pool");

            _transientCache = new List<Database>();

            ResetInMemoryData(machineWide);

            var dbsToDrop = ListPoolDatabases();

            _log.Warning("Dropping databases");
            foreach(var db in dbsToDrop)
            {
                //Clear connection pool
                using(var connection = new SqlConnection(db.ConnectionString(this)))
                {
                    SqlConnection.ClearPool(connection);
                }

                var dropCommand = $"drop database [{db.Name()}]";
                _log.Info(dropCommand);
                _masterConnection.ExecuteNonQuery(dropCommand);
            }

            _log.Warning("Creating new databases");

            InitializePool(machineWide);
        });

        void InitializePool(SharedState machineWide)
        {
            1.Through(30).ForEach(_ => InsertDatabase(machineWide));
        }

        void ResetInMemoryData(SharedState machineWide)
        {
            var reservedDatabases = machineWide.DatabasesReservedBy(_poolId);
            if(reservedDatabases.Any())
            {
                foreach(var reservedDatabase in reservedDatabases)
                {
                    reservedDatabase.Release();
                }
            }
            machineWide.Reset();

            if(_transientCache.Any())
            {
                _transientCache = new List<Database>();
            }
        }

        IReadOnlyList<Database> ListPoolDatabases()
        {
            var databases = new List<string>();
            _masterConnection.UseCommand(
                action: command =>
                        {
                            command.CommandText = "select name from sysdatabases";
                            using(var reader = command.ExecuteReader())
                            {
                                while(reader.Read())
                                {
                                    var dbName = reader.GetString(i: 0);
                                    if(dbName.StartsWith(PoolDatabaseNamePrefix))
                                        databases.Add(dbName);
                                }
                            }
                        });

            return databases.Select(name => new Database(name))
                            .ToList();
        }
    }
}