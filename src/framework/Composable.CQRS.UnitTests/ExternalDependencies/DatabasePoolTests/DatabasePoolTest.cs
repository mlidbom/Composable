using System;
using System.Data;
using Composable.DependencyInjection;
using Composable.Persistence.DB2.SystemExtensions;
using Composable.Persistence.DB2.Testing.Databases;
using Composable.Persistence.MySql.SystemExtensions;
using Composable.Persistence.MySql.Testing.Databases;
using Composable.Persistence.MsSql.SystemExtensions;
using Composable.Persistence.MsSql.Testing.Databases;
using Composable.Persistence.Oracle.SystemExtensions;
using Composable.Persistence.Oracle.Testing.Databases;
using Composable.Persistence.PgSql.SystemExtensions;
using Composable.Persistence.PgSql.Testing.Databases;
using Composable.Testing;
using Composable.Testing.Databases;

namespace Composable.Tests.ExternalDependencies.DatabasePoolTests
{
    //[ConfigurationBasedDuplicateByDimensions]
    public class DatabasePoolTest : DuplicateByPluggableComponentTest
    {
        internal static DatabasePool CreatePool() =>
            TestEnv.PersistenceLayer.Current switch
            {
                PersistenceLayer.MsSql => new MsSqlDatabasePool(),
                PersistenceLayer.MySql => new MySqlDatabasePool(),
                PersistenceLayer.PgSql => new PgSqlDatabasePool(),
                PersistenceLayer.Orcl => new OracleDatabasePool(),
                PersistenceLayer.DB2 => new DB2DatabasePool(),
                PersistenceLayer.Memory => throw new ArgumentOutOfRangeException(),
                _ => throw new ArgumentOutOfRangeException()
            };

        internal static void UseConnection(string connectionString, DatabasePool pool, Action<IDbConnection> func)
        {
            switch(TestEnv.PersistenceLayer.Current)
            {
                case PersistenceLayer.MsSql:
                    UseMsSqlConnection(pool.ConnectionStringFor(connectionString), func);
                    break;
                case PersistenceLayer.PgSql:
                    UsePgSqlConnection(pool.ConnectionStringFor(connectionString), func);
                    break;
                case PersistenceLayer.MySql:
                    UseMySqlConnection(pool.ConnectionStringFor(connectionString), func);
                    break;
                case PersistenceLayer.Orcl:
                    UseOracleConnection(pool.ConnectionStringFor(connectionString), func);
                    break;
                case PersistenceLayer.DB2:
                    UseComposableDB2Connection(pool.ConnectionStringFor(connectionString), func);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        static void UseMySqlConnection(string connectionStringFor, Action<IDbConnection> func) =>
            new MySqlConnectionProvider(connectionStringFor).UseConnection(func);

        static void UsePgSqlConnection(string connectionStringFor, Action<IDbConnection> func) =>
            new PgSqlConnectionProvider(connectionStringFor).UseConnection(conn => func(conn.Connection));

        static void UseMsSqlConnection(string connectionStringFor, Action<IDbConnection> func) =>
            new MsSqlConnectionProvider(connectionStringFor).UseConnection(func);

        static void UseOracleConnection(string connectionStringFor, Action<IDbConnection> func) =>
            new OracleConnectionProvider(connectionStringFor).UseConnection(func);

        static void UseComposableDB2Connection(string connectionStringFor, Action<IDbConnection> func) =>
            new DB2ConnectionProvider(connectionStringFor).UseConnection(conn => func(conn.Connection));

        public DatabasePoolTest(string _) : base(_) {}
    }
}
