using System;
using Npgsql;
using Castle.Core.Internal;
using Composable.Persistence.PgSql.SystemExtensions;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using Composable.Testing.Databases;
#pragma warning disable CA1308 // Normalize strings to uppercase

namespace Composable.Persistence.PgSql.Testing.Databases
{
    sealed class PgSqlDatabasePool : DatabasePool
    {
        readonly PgSqlConnectionProvider _masterConnectionProvider;

        const string ConnectionStringConfigurationParameterName = "COMPOSABLE_PGSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING";
        readonly OptimizedThreadShared<NpgsqlConnectionStringBuilder> _connectionStringBuilder;

        public PgSqlDatabasePool()
        {
            var masterConnectionString = Environment.GetEnvironmentVariable(ConnectionStringConfigurationParameterName)
                                      ?? "Host=localhost;Database=postgres;Username=postgres;Password=Development!1;";

            _masterConnectionProvider = new PgSqlConnectionProvider(masterConnectionString);
            _connectionStringBuilder = new OptimizedThreadShared<NpgsqlConnectionStringBuilder>(new NpgsqlConnectionStringBuilder(masterConnectionString));
        }

        protected override string ConnectionStringFor(Database db)
            => _connectionStringBuilder.WithExclusiveAccess(@this => @this.Mutate(me => me.Database = db.Name.ToLowerInvariant()).ConnectionString);

        protected override void InitReboot() {}

        protected override void EnsureDatabaseExistsAndIsEmpty(Database db)
        {
            var databaseName = db.Name.ToLowerInvariant();
            //Urgent: Figure out PgSql equivalents and if they need to be specified
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
            var exists = (string)_masterConnectionProvider.ExecuteScalar($"SELECT datname FROM pg_database WHERE datname = '{databaseName.ToLowerInvariant()}'");
            if (!exists.IsNullOrEmpty())
            {
                ResetDatabase(db);
            } else
            {
                _masterConnectionProvider?.ExecuteNonQuery($@"CREATE DATABASE {databaseName};");
            }
        }

        protected override void ResetDatabase(Database db) =>
            new PgSqlConnectionProvider(ConnectionStringFor(db)).ExecuteNonQuery(@"
DO $$
DECLARE
        dbRecord RECORD;
BEGIN
	FOR dbRecord IN (SELECT nspname
			FROM pg_catalog.pg_namespace
			WHERE nspname NOT IN ('information_schema', 'pg_catalog', 'pg_toast')) 
	LOOP
			EXECUTE format('DROP SCHEMA %I CASCADE;', dbRecord.nspname);
	END LOOP;

	CREATE SCHEMA public AUTHORIZATION postgres;
	COMMENT ON SCHEMA public IS 'standard public schema';
	GRANT ALL ON SCHEMA public TO PUBLIC;
	GRANT ALL ON SCHEMA public TO postgres;

END; $$;");

        void ResetConnectionPool(Database db)
        {
            using var connection = new NpgsqlConnection(ConnectionStringFor(db));
            NpgsqlConnection.ClearPool(connection);
        }
    }
}
