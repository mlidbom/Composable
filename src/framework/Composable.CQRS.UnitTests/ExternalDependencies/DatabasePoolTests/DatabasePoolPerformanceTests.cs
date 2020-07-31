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
                maxTotal: TestEnv.PersistenceLayer.ValueFor(msSql:150, mySql: 150, pgSql: 150, orcl: 300, db2: 150).Milliseconds());
        }

        [Test]
        public void Multiple_threads_can_reserve_and_release_5_identically_named_databases_in_milliseconds_msSql_50_mySql_75_pgSql_25_orcl_100_db2_50()
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
                maxTotal: TestEnv.PersistenceLayer.ValueFor(msSql:50, mySql: 75, pgSql: 25, orcl: 100, db2:50).Milliseconds());
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
                maxTotal: TestEnv.PersistenceLayer.ValueFor(msSql:125, mySql: 175, pgSql: 400, orcl: 400, db2:100).Milliseconds());
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
                maxTotal: TestEnv.PersistenceLayer.ValueFor(msSql:100, mySql: 100, pgSql: 500, orcl: 300, db2:100).Milliseconds());
        }

        [Test]
        public void Repeated_fetching_of_same_connection_runs_200_times_in_ten_milliseconds()
        {
            if(TestEnv.PersistenceLayer.Current == PersistenceLayer.Memory) return;

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
            if(TestEnv.PersistenceLayer.Current == PersistenceLayer.Memory) return;

            using var manager = CreatePool();
            manager.SetLogLevel(LogLevel.Warning);
            var reservationName = Guid.NewGuid().ToString();
            var connectionsToUse = 100;

            Action useConnection = null;

           switch(TestEnv.PersistenceLayer.Current)
            {
                case PersistenceLayer.MsSql:
                    var msSqlConnectionProvider = MsSqlConnectionPool.CreateInstance(manager.ConnectionStringFor(reservationName));
                    useConnection = () => msSqlConnectionProvider.UseConnection(_ => {});
                    break;
                case PersistenceLayer.Memory:
                    break;
                case PersistenceLayer.MySql:
                    var mySqlConnectionProvider = MySqlConnectionProvider.CreateInstance(manager.ConnectionStringFor(reservationName));
                    useConnection = () => mySqlConnectionProvider.UseConnection(_ => {});
                    break;
                case PersistenceLayer.PgSql:
                    var pgSqlConnectionProvider = new PgSqlConnectionProvider(manager.ConnectionStringFor(reservationName));
                    useConnection = () => pgSqlConnectionProvider.UseConnection(_ => {});
                    break;
                case PersistenceLayer.Orcl:
                    var oracleConnectionProvider = OracleConnectionProvider.CreateInstance(manager.ConnectionStringFor(reservationName));
                    useConnection = () => oracleConnectionProvider .UseConnection(_ => {});
                    break;
                case PersistenceLayer.DB2:
                    var composableDB2ConnectionProvider = DB2ConnectionPool.CreateInstance(manager.ConnectionStringFor(reservationName));
                    useConnection = () => composableDB2ConnectionProvider.UseConnection(_ => {});
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

           useConnection();

           TimeAsserter.Execute(
               action: useConnection!,
               iterations: connectionsToUse,
               maxTotal: TestEnv.PersistenceLayer.ValueFor(msSql:5, mySql: 30, pgSql:1, orcl:10, db2: 50).Milliseconds()
           );
        }

        public DatabasePoolPerformanceTests(string _) : base(_) {}
    }
}
