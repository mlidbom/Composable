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
    //Urgent: Review usage of Serial attribute. Remember: This stops all other tests from running!
    [TestFixture, Performance, Serial]
    public class DatabasePoolPerformanceTests : DatabasePoolTest
    {
        [OneTimeSetUp]public void WarmUpCache()
        {
            using var pool = CreatePool();
            pool.ConnectionStringFor(Guid.NewGuid().ToString());
        }

        [Test]
        public void Single_thread_can_reserve_and_release_10_identically_named_databases_in_milliseconds_msSql_300_mySql_300_pgSql_700_orcl_600()
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
                maxTotal: TestEnvironment.ValueForPersistenceProvider(msSql:300, mySql: 300, pgSql: 700, orcl: 600).Milliseconds());
        }

        [Test]
        public void Multiple_threads_can_reserve_and_release_10_identically_named_databases_in_milliseconds_msSql_100_mySql_150_pgSql_200_orcl_200()
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
                maxTotal: TestEnvironment.ValueForPersistenceProvider(msSql:100, mySql: 150, pgSql: 200, orcl: 200).Milliseconds());
        }

        [Test]
        public void Multiple_threads_can_reserve_and_release_10_differently_named_databases_in_milliseconds_msSql_250_mySql_350_pgSql_800_orcl_800()
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
                maxTotal: TestEnvironment.ValueForPersistenceProvider(msSql:250, mySql: 350, pgSql: 800, orcl: 800).Milliseconds());
        }

        [Test]
        public void Single_thread_can_reserve_and_release_10_differently_named_databases_in_milliseconds_msSql_200_mySql_200_pgSql_1000_orcl_600()
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
                maxTotal: TestEnvironment.ValueForPersistenceProvider(msSql:200, mySql: 200, pgSql: 1000, orcl: 600).Milliseconds());
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
                maxTotal: 10.Milliseconds());
        }

        [Test]
        public void Once_DB_Fetched_Can_use_100_connections_in_milliseconds_MsSql_3_MySql_30_PgSql_1_Oracle_10()
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
               maxTotal: TestEnvironment.ValueForPersistenceProvider(msSql:3.Milliseconds(), mySql: 30.Milliseconds(), pgSql:1.Milliseconds(), orcl:10.Milliseconds())
           );
        }
    }
}
