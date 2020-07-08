using System;
using System.Collections.Generic;
using Npgsql;
using System.Linq;
using Castle.Core.Internal;
using Composable.Contracts;
using Composable.Persistence.PgSql.SystemExtensions;
using Composable.System;
using Composable.Testing.Databases;

namespace Composable.Persistence.PgSql.Testing.Databases
{
    sealed class PgSqlDatabasePool : DatabasePool
    {
        readonly string _masterConnectionString;
        readonly PgSqlConnectionProvider _masterConnectionProvider;

        const string DatabasePgSql = ";Database=postgres;";

        const string ConnectionStringConfigurationParameterName = "COMPOSABLE_PgSql_DATABASE_POOL_MASTER_CONNECTIONSTRING";

        public PgSqlDatabasePool()
        {
            var masterConnectionString = Environment.GetEnvironmentVariable(ConnectionStringConfigurationParameterName);

            _masterConnectionString = masterConnectionString ?? $"Host=localhost{DatabasePgSql}Username=postgres;Password=Development!1;";

            _masterConnectionString = _masterConnectionString.Replace("\\", "_");

            _masterConnectionProvider = new PgSqlConnectionProvider(_masterConnectionString);

            Contract.Assert.That(_masterConnectionString.Contains(DatabasePgSql),
                                 $"Environment variable: {ConnectionStringConfigurationParameterName} connection string must contain the exact string: '{DatabasePgSql}' for technical optimization reasons");
        }

        protected override string ConnectionStringFor(Database db)
            => _masterConnectionString!.Replace(DatabasePgSql, $";Database={db.Name.ToLower()};");

        protected override void EnsureDatabaseExistsAndIsEmpty(Database db)
        {
            var databaseName = db.Name.ToLower();
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
            var exists = (string)_masterConnectionProvider.ExecuteScalar($"SELECT datname FROM pg_database WHERE datname = '{databaseName.ToLower()}'");
            if(!exists.IsNullOrEmpty())
            {
                ResetDatabase(db);
            } else
            {
                _masterConnectionProvider?.ExecuteNonQuery($@"CREATE DATABASE {databaseName};");
            }
        }

        protected override void ResetDatabase(Database db) =>
            new PgSqlConnectionProvider(ConnectionStringFor(db)).ExecuteNonQuery($@"
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
            using var connection = new NpgsqlConnection(this.ConnectionStringFor(db));
            NpgsqlConnection.ClearPool(connection);
        }
    }
}
