using System;
using Composable.Persistence.Common;
using MySql.Data.MySqlClient;
using Composable.Persistence.MySql.SystemExtensions;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using Composable.Testing.Databases;

namespace Composable.Persistence.MySql.Testing.Databases
{
    sealed class MySqlDatabasePool : DatabasePool
    {
        readonly IMySqlConnectionProvider _masterConnectionProvider;

        const string ConnectionStringConfigurationParameterName = "COMPOSABLE_MYSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING";

        readonly OptimizedThreadShared<MySqlConnectionStringBuilder> _connectionStringBuilder;

        public MySqlDatabasePool()
        {
            var masterConnectionString = Environment.GetEnvironmentVariable(ConnectionStringConfigurationParameterName)
                                      ?? "Server=localhost;Database=mysql;Uid=root;Pwd=;";

            _masterConnectionProvider = MySqlConnectionProvider.CreateInstance(masterConnectionString);
            _connectionStringBuilder = new OptimizedThreadShared<MySqlConnectionStringBuilder>(new MySqlConnectionStringBuilder(masterConnectionString));
        }

        protected override string ConnectionStringFor(Database db)
            => _connectionStringBuilder.WithExclusiveAccess(@this => @this.Mutate(me => me.Database = db.Name).ConnectionString);

        protected override void InitReboot() { }

        protected override void EnsureDatabaseExistsAndIsEmpty(Database db)
        {
            var databaseName = db.Name;

            ResetConnectionPool(db);
            _masterConnectionProvider.ExecuteNonQuery($@"
DROP DATABASE IF EXISTS {databaseName};
CREATE DATABASE {databaseName};");
        }

        protected override void ResetDatabase(Database db) =>
            MySqlConnectionProvider.CreateInstance(ConnectionStringFor(db))
                                   .UseCommand(
                                        command => command.SetCommandText($@"
DROP DATABASE {db.Name};
CREATE DATABASE {db.Name};").ExecuteNonQuery());

        void ResetConnectionPool(Database db)
        {
            using var connection = new MySqlConnection(ConnectionStringFor(db));
            MySqlConnection.ClearPool(connection);
        }
    }
}
