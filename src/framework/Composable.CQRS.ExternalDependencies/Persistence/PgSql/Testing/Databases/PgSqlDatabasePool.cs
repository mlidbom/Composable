using System;
using Npgsql;
using Castle.Core.Internal;
using Composable.Persistence.Common.AdoCE;
using Composable.Persistence.PgSql.SystemExtensions;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using Composable.Testing.Databases;
#pragma warning disable CA1308 // Normalize strings to uppercase

namespace Composable.Persistence.PgSql.Testing.Databases
{
    sealed class PgSqlDatabasePool : DatabasePool
    {
        readonly IPgSqlConnectionPool _masterConnectionPool;

        const string ConnectionStringConfigurationParameterName = "COMPOSABLE_PGSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING";
        readonly IThreadShared<NpgsqlConnectionStringBuilder> _connectionStringBuilder;

        public PgSqlDatabasePool()
        {
            var masterConnectionString = Environment.GetEnvironmentVariable(ConnectionStringConfigurationParameterName)
                                      ?? "Host=localhost;Database=postgres;Username=postgres;Password=Development!1;";

            _masterConnectionPool = IPgSqlConnectionPool.CreateInstance(masterConnectionString);
            _connectionStringBuilder = ThreadShared.WithDefaultTimeout<NpgsqlConnectionStringBuilder>(new NpgsqlConnectionStringBuilder(masterConnectionString));
        }

        protected override string ConnectionStringFor(Database db)
            => _connectionStringBuilder.Update(@this => @this.Mutate(me => me.Database = db.Name.ToLowerInvariant()).ConnectionString);

        protected override void EnsureDatabaseExistsAndIsEmpty(Database db)
        {
            var databaseName = db.Name.ToLowerInvariant();
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
            var exists = (string)_masterConnectionPool.ExecuteScalar($"SELECT datname FROM pg_database WHERE datname = '{databaseName.ToLowerInvariant()}'");
            if (!exists.IsNullOrEmpty())
            {
                ResetDatabase(db);
            } else
            {
                _masterConnectionPool?.ExecuteNonQuery($@"CREATE DATABASE {databaseName};");
            }
        }

        protected override void ResetDatabase(Database db) =>
            IPgSqlConnectionPool.CreateInstance(ConnectionStringFor(db)).UseCommand(
                command => command.SetCommandText(@"
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

END; $$;")
                                  .PrepareStatement()
                                  .ExecuteNonQuery());

        void ResetConnectionPool(Database db)
        {
            using var connection = new NpgsqlConnection(ConnectionStringFor(db));
            NpgsqlConnection.ClearPool(connection);
        }
    }
}
