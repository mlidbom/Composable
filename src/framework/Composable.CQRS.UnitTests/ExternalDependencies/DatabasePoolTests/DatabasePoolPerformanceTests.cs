using System;
using Composable.DependencyInjection;
using Composable.Logging;
using Composable.Persistence.DB2.SystemExtensions;
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
    //Urgent: Consider whether these tests should run all the time. Don't they sort of mess up the performance of the pool by involving more databases than necessary, growing the connection pools, trashing cache locality etc?
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
        public void Single_thread_can_reserve_and_release_5_identically_named_databases_in_milliseconds_msSql_150_mySql_150_pgSql_150_orcl_300_db2_150()
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
                iterations: 5,
                maxTotal: TestEnvironment.ValueForPersistenceProvider(msSql:150, mySql: 150, pgSql: 150, orcl: 300, db2: 150).Milliseconds());
        }

        [Test]
        public void Multiple_threads_can_reserve_and_release_5_identically_named_databases_in_milliseconds_msSql_50_mySql_75_pgSql_25_orcl_100_db2_50()
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
                iterations: 5,
                maxTotal: TestEnvironment.ValueForPersistenceProvider(msSql:50, mySql: 75, pgSql: 25, orcl: 100, db2:50).Milliseconds());
        }

        [Test]
        public void Multiple_threads_can_reserve_and_release_5_differently_named_databases_in_milliseconds_msSql_125_mySql_175_pgSql_400_orcl_400_db2_100()
        {
            TimeAsserter.ExecuteThreaded(
                action: () =>
                       {
                           using var manager = CreatePool();
                           manager.SetLogLevel(LogLevel.Warning);
                           manager.ConnectionStringFor(Guid.NewGuid().ToString());
                       },
                iterations: 5,
                maxTotal: TestEnvironment.ValueForPersistenceProvider(msSql:125, mySql: 175, pgSql: 400, orcl: 400, db2:100).Milliseconds());
        }

        [Test]
        public void Single_thread_can_reserve_and_release_5_differently_named_databases_in_milliseconds_msSql_100_mySql_100_pgSql_500_orcl_300_db2_100()
        {
            TimeAsserter.Execute(
                action: () =>
                       {
                           using var manager = CreatePool();
                           manager.SetLogLevel(LogLevel.Warning);
                           manager.ConnectionStringFor(Guid.NewGuid().ToString());
                       },
                iterations: 5,
                maxTotal: TestEnvironment.ValueForPersistenceProvider(msSql:100, mySql: 100, pgSql: 500, orcl: 300, db2:100).Milliseconds());
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
        public void Once_DB_Fetched_Can_use_100_connections_in_milliseconds_MsSql_5_MySql_30_PgSql_1_Oracle_10_db2_50()
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
                case PersistenceLayer.DB2:
                    var composableDB2ConnectionProvider = new ComposableDB2ConnectionProvider(manager.ConnectionStringFor(reservationName));
                    useConnection = () => composableDB2ConnectionProvider .UseConnection(_ => {});
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

           useConnection();

           TimeAsserter.Execute(
               action: useConnection!,
               iterations: connectionsToUse,
               maxTotal: TestEnvironment.ValueForPersistenceProvider(msSql:5, mySql: 30, pgSql:1, orcl:10, db2: 50).Milliseconds()
           );
        }
    }
}
