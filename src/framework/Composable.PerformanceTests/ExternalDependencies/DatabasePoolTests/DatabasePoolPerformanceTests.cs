using System;
using Composable.DependencyInjection;
using Composable.Logging;
using Composable.Persistence.DB2.SystemExtensions;
using Composable.Persistence.MySql.SystemExtensions;
using Composable.Persistence.MsSql.SystemExtensions;
using Composable.Persistence.Oracle.SystemExtensions;
using Composable.Persistence.PgSql.SystemExtensions;
using Composable.SystemCE;
using Composable.Testing;
using Composable.Testing.Performance;
using NCrunch.Framework;
using NUnit.Framework;

namespace Composable.Tests.ExternalDependencies.DatabasePoolTests
{
    //Performance: Review usage of Serial attribute. Remember: This stops all other tests from running!
    [Performance, Serial]
    public class DatabasePoolPerformanceTests : DatabasePoolTest
    {
        [OneTimeSetUp]public void WarmUpCache()
        {
            if(TestEnv.PersistenceLayer.Current == PersistenceLayer.Memory) return;
            using var pool = CreatePool();
            pool.ConnectionStringFor(Guid.NewGuid().ToString());
        }

        [Test]
        public void Single_thread_can_reserve_and_release_5_identically_named_databases_in_milliseconds_msSql_150_mySql_150_pgSql_150_orcl_300_db2_150()
        {
            if(TestEnv.PersistenceLayer.Current == PersistenceLayer.Memory) return;

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
                maxTotal: TestEnv.PersistenceLayer.ValueFor(db2: 150, msSql: 150, mySql: 150, orcl: 300, pgSql: 150).Milliseconds());
        }

        [Test]
        public void Multiple_threads_can_reserve_and_release_5_identically_named_databases_in_milliseconds_db2_50_msSql_50_mySql_75_orcl_100_pgSql_25()
        {
            if(TestEnv.PersistenceLayer.Current == PersistenceLayer.Memory) return;

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
                maxTotal: TestEnv.PersistenceLayer.ValueFor(db2: 50, msSql: 50, mySql: 75, orcl: 100, pgSql: 25).Milliseconds());
        }

        [Test]
        public void Multiple_threads_can_reserve_and_release_5_differently_named_databases_in_milliseconds_msSql_125_mySql_175_pgSql_400_orcl_400_db2_100()
        {
            if(TestEnv.PersistenceLayer.Current == PersistenceLayer.Memory) return;

            TimeAsserter.ExecuteThreaded(
                action: () =>
                       {
                           using var manager = CreatePool();
                           manager.SetLogLevel(LogLevel.Warning);
                           manager.ConnectionStringFor(Guid.NewGuid().ToString());
                       },
                iterations: 5,
                maxTotal: TestEnv.PersistenceLayer.ValueFor(db2: 100, msSql: 125, mySql: 175, orcl: 400, pgSql: 400).Milliseconds());
        }

        [Test]
        public void Single_thread_can_reserve_and_release_5_differently_named_databases_in_milliseconds_msSql_100_mySql_100_pgSql_500_orcl_300_db2_100()
        {
            if(TestEnv.PersistenceLayer.Current == PersistenceLayer.Memory) return;

            TimeAsserter.Execute(
                action: () =>
                       {
                           using var manager = CreatePool();
                           manager.SetLogLevel(LogLevel.Warning);
                           manager.ConnectionStringFor(Guid.NewGuid().ToString());
                       },
                iterations: 5,
                maxTotal: TestEnv.PersistenceLayer.ValueFor(db2: 100, msSql: 100, mySql: 100, orcl: 300, pgSql: 500).Milliseconds());
        }

        [Test]
        public void Repeated_fetching_of_same_connection_runs_20_times_in_1_milliseconds()
        {
            if(TestEnv.PersistenceLayer.Current == PersistenceLayer.Memory) return;

            var dbName = Guid.NewGuid().ToString();
            using var manager = CreatePool();
            manager.SetLogLevel(LogLevel.Warning);
            manager.ConnectionStringFor(dbName);

            TimeAsserter.Execute(
                action: () => manager.ConnectionStringFor(dbName),
                iterations: 20,
                maxTotal: 1.Milliseconds());
        }

        [Test]
        public void Once_DB_Fetched_Can_use_XX_connections_in_1_millisecond_db2_2_MsSql_25_MySql_2_Oracle_7_PgSql_40()
        {
            if(TestEnv.PersistenceLayer.Current == PersistenceLayer.Memory) return;

            using var manager = CreatePool();
            manager.SetLogLevel(LogLevel.Warning);
            var reservationName = Guid.NewGuid().ToString();

            Action useConnection = null;

           switch(TestEnv.PersistenceLayer.Current)
            {
                case PersistenceLayer.MsSql:
                    var msSqlConnectionProvider = IMsSqlConnectionPool.CreateInstance(manager.ConnectionStringFor(reservationName));
                    useConnection = () => msSqlConnectionProvider.UseConnection(_ => {});
                    break;
                case PersistenceLayer.Memory:
                    break;
                case PersistenceLayer.MySql:
                    var mySqlConnectionProvider = IMySqlConnectionPool.CreateInstance(manager.ConnectionStringFor(reservationName));
                    useConnection = () => mySqlConnectionProvider.UseConnection(_ => {});
                    break;
                case PersistenceLayer.PgSql:
                    var pgSqlConnectionProvider = IPgSqlConnectionPool.CreateInstance(manager.ConnectionStringFor(reservationName));
                    useConnection = () => pgSqlConnectionProvider.UseConnection(_ => {});
                    break;
                case PersistenceLayer.Orcl:
                    var oracleConnectionProvider = IOracleConnectionPool.CreateInstance(manager.ConnectionStringFor(reservationName));
                    useConnection = () => oracleConnectionProvider .UseConnection(_ => {});
                    break;
                case PersistenceLayer.DB2:
                    var composableDB2ConnectionProvider = IDB2ConnectionPool.CreateInstance(manager.ConnectionStringFor(reservationName));
                    useConnection = () => composableDB2ConnectionProvider.UseConnection(_ => {});
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

           useConnection();

           TimeAsserter.Execute(
               action: useConnection!,
               maxTotal: 1.Milliseconds(),
               iterations : TestEnv.PersistenceLayer.ValueFor(db2: 2, msSql: 25, mySql: 2, orcl: 7, pgSql: 40)
           );
        }

        public DatabasePoolPerformanceTests(string _) : base(_) {}
    }
}
