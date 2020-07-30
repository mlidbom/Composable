using System;
using MySql.Data.MySqlClient;
using Composable.Persistence.MySql.SystemExtensions;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using Composable.Testing.Databases;

namespace Composable.Persistence.MySql.Testing.Databases
{
    sealed class MySqlDatabasePool : DatabasePool
    {
        readonly MySqlConnectionProvider _masterConnectionProvider;

        const string ConnectionStringConfigurationParameterName = "COMPOSABLE_MYSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING";

        readonly OptimizedThreadShared<MySqlConnectionStringBuilder> _connectionStringBuilder;

        public MySqlDatabasePool()
        {
            var masterConnectionString = Environment.GetEnvironmentVariable(ConnectionStringConfigurationParameterName)
                                      ?? "Server=localhost;Database=mysql;Uid=root;Pwd=;";

            _masterConnectionProvider = new MySqlConnectionProvider(masterConnectionString);
            _connectionStringBuilder = new OptimizedThreadShared<MySqlConnectionStringBuilder>(new MySqlConnectionStringBuilder(masterConnectionString));
        }

        protected override string ConnectionStringFor(Database db)
            => _connectionStringBuilder.WithExclusiveAccess(@this => @this.Mutate(me => me.Database = db.Name).ConnectionString);

        protected override void InitReboot() { }

        protected override void EnsureDatabaseExistsAndIsEmpty(Database db)
        {
            var databaseName = db.Name;
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
            new MySqlConnectionProvider(ConnectionStringFor(db)).ExecuteNonQuery($@"
DROP DATABASE {db.Name};
CREATE DATABASE {db.Name};");

        void ResetConnectionPool(Database db)
        {
            using var connection = new MySqlConnection(ConnectionStringFor(db));
            MySqlConnection.ClearPool(connection);
        }
    }
}
