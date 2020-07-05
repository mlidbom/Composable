using System;
using System.Data;
using Composable.DependencyInjection;
using Composable.Persistence.MySql.SystemExtensions;
using Composable.Persistence.MySql.Testing.Databases;
using Composable.Persistence.SqlServer.SystemExtensions;
using Composable.Persistence.SqlServer.Testing.Databases;
using Composable.Testing;
using Composable.Testing.Databases;
using Composable.Testing.Performance;
using NCrunch.Framework;

namespace Composable.Tests.ExternalDependencies.DatabasePoolTests
{
    [DuplicateByDimensions(nameof(PersistenceLayer.SqlServer), nameof(PersistenceLayer.MySql))]
    public class DatabasePoolTest
    {
        internal DatabasePool CreatePool() =>
            TestEnvironment.TestingPersistenceLayer switch
            {
                PersistenceLayer.SqlServer => new SqlServerDatabasePool(),
                PersistenceLayer.MySql => new MySqlDatabasePool(),
                PersistenceLayer.InMemory => throw new ArgumentOutOfRangeException(),
                _ => throw new ArgumentOutOfRangeException()
            };

        internal static void UseConnection(string connectionString, DatabasePool pool, Action<IDbConnection> func)
        {
            switch(TestEnvironment.TestingPersistenceLayer)
            {
                case PersistenceLayer.SqlServer:
                    UseSqlServerConnection(pool.ConnectionStringFor(connectionString), func);
                    break;
                case PersistenceLayer.InMemory:
                    UseMySqlConnection(pool.ConnectionStringFor(connectionString), func);
                    break;
                case PersistenceLayer.MySql:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        static void UseMySqlConnection(string connectionStringFor, Action<IDbConnection> func) =>
            new MySqlConnectionProvider(connectionStringFor).UseConnection(func);

        static void UseSqlServerConnection(string connectionStringFor, Action<IDbConnection> func) =>
            new SqlServerConnectionProvider(connectionStringFor).UseConnection(func);
    }
}
