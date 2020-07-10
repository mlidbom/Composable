using System;
using System.Data;
using Composable.DependencyInjection;
using Composable.Persistence.MySql.SystemExtensions;
using Composable.Persistence.MySql.Testing.Databases;
using Composable.Persistence.MsSql.SystemExtensions;
using Composable.Persistence.MsSql.Testing.Databases;
using Composable.Persistence.PgSql.SystemExtensions;
using Composable.Persistence.PgSql.Testing.Databases;
using Composable.Testing;
using Composable.Testing.Databases;
using Composable.Testing.Performance;
using NCrunch.Framework;

namespace Composable.Tests.ExternalDependencies.DatabasePoolTests
{
    [DuplicateByDimensions(nameof(PersistenceLayer.MsSql), nameof(PersistenceLayer.MySql), nameof(PersistenceLayer.PgSql))]
    public class DatabasePoolTest
    {
        internal static DatabasePool CreatePool() =>
            TestEnvironment.TestingPersistenceLayer switch
            {
                PersistenceLayer.MsSql => new MsSqlDatabasePool(),
                PersistenceLayer.MySql => new MySqlDatabasePool(),
                PersistenceLayer.PgSql => new PgSqlDatabasePool(),
                PersistenceLayer.InMemory => throw new ArgumentOutOfRangeException(),
                _ => throw new ArgumentOutOfRangeException()
            };

        internal static void UseConnection(string connectionString, DatabasePool pool, Action<IDbConnection> func)
        {
            switch(TestEnvironment.TestingPersistenceLayer)
            {
                case PersistenceLayer.MsSql:
                    UseMsSqlConnection(pool.ConnectionStringFor(connectionString), func);
                    break;
                case PersistenceLayer.InMemory:
                    UseMySqlConnection(pool.ConnectionStringFor(connectionString), func);
                    break;
                case PersistenceLayer.PgSql:
                    UsePgSqlConnection(pool.ConnectionStringFor(connectionString), func);
                    break;
                case PersistenceLayer.MySql:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        static void UseMySqlConnection(string connectionStringFor, Action<IDbConnection> func) =>
            new MySqlConnectionProvider(connectionStringFor).UseConnection(func);

        static void UsePgSqlConnection(string connectionStringFor, Action<IDbConnection> func) =>
            new PgSqlConnectionProvider(connectionStringFor).UseConnection(func);

        static void UseMsSqlConnection(string connectionStringFor, Action<IDbConnection> func) =>
            new MsSqlConnectionProvider(connectionStringFor).UseConnection(func);
    }
}
