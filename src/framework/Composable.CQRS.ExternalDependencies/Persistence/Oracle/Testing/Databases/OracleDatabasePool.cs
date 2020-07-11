using System;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using System.Linq;
using Composable.Contracts;
using Composable.Persistence.Oracle.SystemExtensions;
using Composable.System;
using Composable.Testing.Databases;

namespace Composable.Persistence.Oracle.Testing.Databases
{
    sealed class OracleDatabasePool : DatabasePool
    {
        readonly string _masterConnectionString;
        readonly OracleConnectionProvider _masterConnectionProvider;

        const string DatabaseOracle = ";Database=mysql;";

        const string ConnectionStringConfigurationParameterName = "COMPOSABLE_MYSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING";

        public OracleDatabasePool()
        {
            var masterConnectionString = Environment.GetEnvironmentVariable(ConnectionStringConfigurationParameterName);

            _masterConnectionString = masterConnectionString ?? $"Server=localhost{DatabaseOracle}Uid=root;Pwd=;";

            _masterConnectionString = _masterConnectionString.Replace("\\", "_");

            _masterConnectionProvider = new OracleConnectionProvider(_masterConnectionString);

            Contract.Assert.That(_masterConnectionString.Contains(DatabaseOracle),
                                 $"Environment variable: {ConnectionStringConfigurationParameterName} connection string must contain the exact string: '{DatabaseOracle}' for technical optimization reasons");
        }

        protected override string ConnectionStringFor(Database db)
            => _masterConnectionString!.Replace(DatabaseOracle, $";Database={db.Name};");

        protected override void EnsureDatabaseExistsAndIsEmpty(Database db)
        {
            var databaseName = db.Name;
            //Urgent: Figure out Oracle equivalents and if they need to be specified
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
            new OracleConnectionProvider(this.ConnectionStringFor(db)).ExecuteNonQuery($@"
DROP DATABASE {db.Name};
CREATE DATABASE {db.Name};");

        void ResetConnectionPool(Database db)
        {
            using var connection = new OracleConnection(this.ConnectionStringFor(db));
            OracleConnection.ClearPool(connection);
        }
    }
}
