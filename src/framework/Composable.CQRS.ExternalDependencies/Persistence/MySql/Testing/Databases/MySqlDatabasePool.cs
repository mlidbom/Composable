using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using System.Linq;
using Composable.Contracts;
using Composable.Persistence.MySql.SystemExtensions;
using Composable.System;
using Composable.System.Transactions;
using Composable.Testing.Databases;

namespace Composable.Persistence.MySql.Testing.Databases
{
    sealed class MySqlDatabasePool : DatabasePool
    {
        readonly string _masterConnectionString;
        readonly MySqlConnectionProvider _masterConnectionProvider;

        const string DatabaseMySql = ";Database=mysql;";

        const string ConnectionStringConfigurationParameterName = "COMPOSABLE_MYSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING";

        public MySqlDatabasePool()
        {
            var masterConnectionString = Environment.GetEnvironmentVariable(ConnectionStringConfigurationParameterName);

            _masterConnectionString = masterConnectionString ?? $"Server=localhost{DatabaseMySql}Uid=root;Pwd=;";

            _masterConnectionString = _masterConnectionString.Replace("\\", "_");

            _masterConnectionProvider = new MySqlConnectionProvider(_masterConnectionString);

            Contract.Assert.That(_masterConnectionString.Contains(DatabaseMySql),
                                 $"Environment variable: {ConnectionStringConfigurationParameterName} connection string must contain the exact string: '{DatabaseMySql}' for technical optimization reasons");
        }

        protected internal override string ConnectionStringForDbNamed(string dbName)
            => _masterConnectionString!.Replace(DatabaseMySql, $";Database={dbName};");

        protected override void CreateDatabase(string databaseName)
        {
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

            _masterConnectionProvider?.ExecuteNonQuery($@"
DROP DATABASE IF EXISTS {databaseName};
CREATE DATABASE {databaseName};");
        }

        protected override void DropDatabase(Database db) =>
            _masterConnectionProvider?.ExecuteNonQuery($@"

DROP DATABASE IF EXISTS {db.Name()};");

        protected override IReadOnlyList<Database> ListPoolDatabases()
            => _masterConnectionProvider
               .UseCommand(command => command.SetCommandText("SHOW DATABASES;")
                                             .ExecuteReaderAndSelect(reader => reader.GetString(0))
                                             .Where(dbName => dbName.StartsWith(PoolDatabaseNamePrefix))
                                             .Select(dbName => new Database(dbName))
                                             .ToList());

        protected override void ResetDatabase(Database db) =>
            TransactionScopeCe.SuppressAmbient(
                () => new MySqlConnectionProvider(db.ConnectionString(this))
                   .UseConnection(action: connection => connection.DropAllObjectsAndSetReadCommittedSnapshotIsolationLevel(db.Name())));

        protected override void ResetConnectionPool(Database db)
        {
            using var connection = new MySqlConnection(db.ConnectionString(this));
            MySqlConnection.ClearPool(connection);
        }
    }
}
