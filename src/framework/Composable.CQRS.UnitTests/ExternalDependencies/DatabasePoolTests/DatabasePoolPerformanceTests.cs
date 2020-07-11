using System;
using Composable.DependencyInjection;
using Composable.Logging;
using Composable.Persistence.MySql.SystemExtensions;
using Composable.Persistence.MsSql.SystemExtensions;
using Composable.Persistence.Oracle.SystemExtensions;
using Composable.Persistence.PgSql.SystemExtensions;
using Composable.System;
using Composable.Testing;
using Composable.Testing.Databases;
using Composable.Testing.Performance;
using NCrunch.Framework;
using NUnit.Framework;
using Oracle.ManagedDataAccess.Types;

namespace Composable.Tests.ExternalDependencies.DatabasePoolTests
{
    [TestFixture, Performance, Serial]
    public class DatabasePoolPerformanceTests : DatabasePoolTest
    {
        [OneTimeSetUp]public void WarmUpCache()
        {
            using var pool = CreatePool();
            pool.ConnectionStringFor(Guid.NewGuid().ToString());
        }

        [Test]
        public void Single_thread_can_reserve_and_release_10_identically_named_databases_in_300_milliseconds_Orcl_700_milliseconds()
        {
            var dbName = Guid.NewGuid().ToString();

            TimeAsserter.Execute(
                action:
                () =>
                {
                    using var manager = CreatePool();
                    manager.SetLogLevel(LogLevel.Warning);
                    manager.ConnectionStringFor(dbName);
                },
                iterations: 10,
                maxTotal: TestEnvironment.ValueForPersistenceProvider(fallback: 300.Milliseconds(), orcl: 700.Milliseconds()));
        }

        [Test]
        public void Multiple_threads_can_reserve_and_release_10_identically_named_databases_in_70_milliseconds_Orcl_4000_milliseconds()
        {
            var dbName = Guid.NewGuid().ToString();

            TimeAsserter.ExecuteThreaded(
                action:
                () =>
                {
                    using var manager = CreatePool();
                    manager.SetLogLevel(LogLevel.Warning);
                    manager.ConnectionStringFor(dbName);
                },
                iterations: 10,
                maxTotal: TestEnvironment.ValueForPersistenceProvider(fallback: 70.Milliseconds(), orcl: 4000.Milliseconds()));
        }

        [Test]
        public void Multiple_threads_can_reserve_and_release_10_differently_named_databases_in_300_milliseconds_Orcl_700_milliseconds()
        {
            DatabasePool manager = null;

            TimeAsserter.ExecuteThreaded(
                setup: () =>
                       {
                           manager = CreatePool();
                           manager.SetLogLevel(LogLevel.Warning);
                           manager.ConnectionStringFor("fake_to_force_creation_of_manager_database");
                       },
                tearDown: () => manager.Dispose(),
                action: () => manager.ConnectionStringFor(Guid.NewGuid().ToString()),
                iterations: 10,
                maxTotal: TestEnvironment.ValueForPersistenceProvider(fallback: 300.Milliseconds(), orcl:700.Milliseconds())
            );
        }

        [Test]
        public void Single_thread_can_reserve_and_release_10_differently_named_databases_in_300_milliseconds_Orcl_700_milliseconds()
        {
            DatabasePool manager = null;

            TimeAsserter.Execute(
                setup: () =>
                       {
                           manager = CreatePool();
                           manager.SetLogLevel(LogLevel.Warning);
                           manager.ConnectionStringFor("fake_to_force_creation_of_manager_database");
                       },
                tearDown: () => manager.Dispose(),
                action: () => manager.ConnectionStringFor(Guid.NewGuid().ToString()),
                iterations: 10,
                maxTotal: TestEnvironment.ValueForPersistenceProvider(fallback: 300.Milliseconds(), orcl:700.Milliseconds())
            );
        }

        [Test]
        public void Repeated_fetching_of_same_connection_runs_200_times_in_ten_milliseconds()
        {
            var dbName = Guid.NewGuid().ToString();
            using var manager = CreatePool();
            manager.SetLogLevel(LogLevel.Warning);
            manager.ConnectionStringFor(dbName);

            TimeAsserter.Execute(
                action: () => manager.ConnectionStringFor(dbName),
                iterations: 200,
                maxTotal: 10.Milliseconds()
            );
        }

        [Test]
        public void Once_DB_Fetched_MsSql_Can_use_100_connections_in_2_milliseconds_MySql_25_milliseconds_PgSql_1_millisecond_Oracle_50_milliseconds()
        {
            using var manager = CreatePool();
            manager.SetLogLevel(LogLevel.Warning);
            var reservationName = Guid.NewGuid().ToString();
            var connectionsToUse = 100;

            Action useConnection = null;

           switch(TestEnvironment.TestingPersistenceLayer)
            {
                case PersistenceLayer.MsSql:
                    var msSqlConnectionProvider = new MsSqlConnectionProvider(manager.ConnectionStringFor(reservationName));
                    useConnection = () => msSqlConnectionProvider.UseConnection(_ => {});
                    break;
                case PersistenceLayer.InMemory:
                    break;
                case PersistenceLayer.MySql:
                    var mySqlConnectionProvider = new MySqlConnectionProvider(manager.ConnectionStringFor(reservationName));
                    useConnection = () => mySqlConnectionProvider.UseConnection(_ => {});
                    break;
                case PersistenceLayer.PgSql:
                    var pgSqlConnectionProvider = new PgSqlConnectionProvider(manager.ConnectionStringFor(reservationName));
                    useConnection = () => pgSqlConnectionProvider.UseConnection(_ => {});
                    break;
                case PersistenceLayer.Orcl:
                    var oracleConnectionProvider = new OracleConnectionProvider(manager.ConnectionStringFor(reservationName));
                    useConnection = () => oracleConnectionProvider .UseConnection(_ => {});
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

           useConnection();

           TimeAsserter.Execute(
               action: useConnection!,
               iterations: connectionsToUse,
               maxTotal: TestEnvironment.ValueForPersistenceProvider(msSql:2.Milliseconds(), mySql: 25.Milliseconds(), pgSql:1.Milliseconds(), orcl:50.Milliseconds())
           );
        }
    }
}
