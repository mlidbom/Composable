using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Composable.Contracts;
using Composable.Persistence.MsSql.SystemExtensions;
using Composable.System;
using Composable.Testing.Databases;

namespace Composable.Persistence.MsSql.Testing.Databases
{
    class MsSqlDatabasePool : DatabasePool
    {
        readonly string _masterConnectionString;
        readonly MsSqlConnectionProvider _masterConnectionProvider;

        const string ConnectionStringConfigurationParameterName = "COMPOSABLE_SQL_SERVER_DATABASE_POOL_MASTER_CONNECTIONSTRING";

        public MsSqlDatabasePool()
        {
            var masterConnectionString = Environment.GetEnvironmentVariable(ConnectionStringConfigurationParameterName);

            _masterConnectionString = masterConnectionString ?? $"Data Source=localhost;Initial Catalog=master;Integrated Security=True;";

            _masterConnectionString = _masterConnectionString.Replace(oldValue: "\\", newValue: "_");

            _masterConnectionProvider = new MsSqlConnectionProvider(_masterConnectionString);
        }

        protected override string ConnectionStringFor(Database db)
            => new SqlConnectionStringBuilder(_masterConnectionString){InitialCatalog = db.Name}.ConnectionString;

        protected override void EnsureDatabaseExistsAndIsEmpty(Database db)
        {
            var databaseName = db.Name;
            var exists = (string)_masterConnectionProvider.ExecuteScalar($"select name from sysdatabases where name = '{databaseName}'") == databaseName;
            if(exists)
            {
                ResetDatabase(db);
            } else
            {
                ResetConnectionPool(db);
                var createDatabaseCommand = $@"CREATE DATABASE [{databaseName}]";
                if(!DatabaseRootFolderOverride.IsNullEmptyOrWhiteSpace())
                    createDatabaseCommand += $@"
ON      ( NAME = {databaseName}_data, FILENAME = '{DatabaseRootFolderOverride}\{databaseName}.mdf') 
LOG ON  ( NAME = {databaseName}_log, FILENAME = '{DatabaseRootFolderOverride}\{databaseName}.ldf');";

                createDatabaseCommand += $@"
ALTER DATABASE [{databaseName}] SET RECOVERY SIMPLE;
ALTER DATABASE[{databaseName}] SET READ_COMMITTED_SNAPSHOT ON";

                _masterConnectionProvider!.ExecuteNonQuery(createDatabaseCommand);
            }
        }

        protected override void ResetDatabase(Database db) =>
            new MsSqlConnectionProvider(this.ConnectionStringFor(db))
               .UseConnection(action: connection => connection.DropAllObjectsAndSetReadCommittedSnapshotIsolationLevel());

        protected void ResetConnectionPool(Database db)
        {
            using var connection = new SqlConnection(this.ConnectionStringFor(db));
            SqlConnection.ClearPool(connection);
        }
    }
}
