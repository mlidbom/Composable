using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using System.Linq;
using Composable.Contracts;
using Composable.Persistence.MySql.SystemExtensions;
using Composable.System;
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

        protected override string ConnectionStringFor(Database db)
            => _masterConnectionString!.Replace(DatabaseMySql, $";Database={db.Name};");

        protected override void EnsureDatabaseExistsAndIsEmpty(Database db)
        {
            var databaseName = db.Name;
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

            ResetConnectionPool(db);
            _masterConnectionProvider?.ExecuteNonQuery($@"
DROP DATABASE IF EXISTS {databaseName};
CREATE DATABASE {databaseName};");
        }

        protected override void ResetDatabase(Database db) =>
            new MySqlConnectionProvider(this.ConnectionStringFor(db)).ExecuteNonQuery($@"
DROP DATABASE {db.Name};
CREATE DATABASE {db.Name};");

        void ResetConnectionPool(Database db)
        {
            using var connection = new MySqlConnection(this.ConnectionStringFor(db));
            MySqlConnection.ClearPool(connection);
        }
    }
}
