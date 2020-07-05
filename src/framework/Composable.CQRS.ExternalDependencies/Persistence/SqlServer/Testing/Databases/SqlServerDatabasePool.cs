﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Composable.Contracts;
using Composable.Persistence.SqlServer.SystemExtensions;
using Composable.System;
using Composable.System.Transactions;
using Composable.Testing.Databases;

namespace Composable.Persistence.SqlServer.Testing.Databases
{
    class SqlServerDatabasePool : DatabasePool
    {
        readonly string _masterConnectionString;
        readonly SqlServerConnectionProvider _masterConnectionProvider;

        const string InitialCatalogMaster = ";Initial Catalog=master;";

        const string ConnectionStringConfigurationParameterName = "COMPOSABLE_SQL_SERVER_DATABASE_POOL_MASTER_CONNECTIONSTRING";

        public SqlServerDatabasePool()
        {
            var masterConnectionString = Environment.GetEnvironmentVariable(ConnectionStringConfigurationParameterName);

            _masterConnectionString = masterConnectionString ?? $"Data Source=localhost{InitialCatalogMaster}Integrated Security=True;";

            _masterConnectionString = _masterConnectionString.Replace("\\", "_");

            _masterConnectionProvider = new SqlServerConnectionProvider(_masterConnectionString);

            Contract.Assert.That(_masterConnectionString.Contains(InitialCatalogMaster),
                                 $"Environment variable: {ConnectionStringConfigurationParameterName} connection string must contain the exact string: '{InitialCatalogMaster}' for technical optimization reasons");
        }

        protected internal override string ConnectionStringForDbNamed(string dbName)
            => _masterConnectionString!.Replace(InitialCatalogMaster, $";Initial Catalog={dbName};");

        protected override void CreateDatabase(string databaseName)
        {
            var createDatabaseCommand = $@"CREATE DATABASE [{databaseName}]";
            if(!DatabaseRootFolderOverride.IsNullEmptyOrWhiteSpace())
            {
                createDatabaseCommand += $@"
ON      ( NAME = {databaseName}_data, FILENAME = '{DatabaseRootFolderOverride}\{databaseName}.mdf') 
LOG ON  ( NAME = {databaseName}_log, FILENAME = '{DatabaseRootFolderOverride}\{databaseName}.ldf');";
            }

            createDatabaseCommand += $@"
ALTER DATABASE [{databaseName}] SET RECOVERY SIMPLE;
ALTER DATABASE[{ databaseName}] SET READ_COMMITTED_SNAPSHOT ON";

            _masterConnectionProvider!.ExecuteNonQuery(createDatabaseCommand);
        }

        protected override void DropDatabase(Database db)
        {
            var dropCommand = $@"
alter database [{db.Name()}] set single_user with rollback immediate
drop database [{db.Name()}]";
            _log.Info(dropCommand);
            _masterConnectionProvider?.ExecuteNonQuery(dropCommand);
        }

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
                () => new SqlServerConnectionProvider(db.ConnectionString(this))
                   .UseConnection(action: connection => connection.DropAllObjectsAndSetReadCommittedSnapshotIsolationLevel()));
        }
        protected override void ResetConnectionPool(Database db)
        {
            using(var connection = new SqlConnection(db.ConnectionString(this)))
            {
                SqlConnection.ClearPool(connection);
            }
        }
    }
}
