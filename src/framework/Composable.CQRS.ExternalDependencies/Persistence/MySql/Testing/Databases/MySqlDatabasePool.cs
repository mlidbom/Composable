using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Transactions;
using Composable.Contracts;
using Composable.Logging;
using Composable.Persistence.MySql.SystemExtensions;
using Composable.System;
using Composable.System.Reflection;
using Composable.System.Threading;
using Composable.System.Threading.ResourceAccess;
using Composable.System.Transactions;
using Composable.Testing.Databases;
using MySql.Data.MySqlClient;

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
            _masterConnectionProvider?.ExecuteNonQuery($@"DROP DATABASE IF EXISTS {db.Name()};");

        protected override IReadOnlyList<Database> ListPoolDatabases()
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

        protected override void ResetDatabase(Database db)
        {
            TransactionScopeCe.SuppressAmbient(
                () => new MySqlConnectionProvider(db.ConnectionString(this))
                   .UseConnection(action: connection => connection.DropAllObjectsAndSetReadCommittedSnapshotIsolationLevel(db.Name())));
        }

        protected override void ResetConnectionPool(Database db)
        {
            using var connection = new MySqlConnection(db.ConnectionString(this));
            MySqlConnection.ClearPool(connection);
        }
    }
}
