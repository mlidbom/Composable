using System;
using Composable.DependencyInjection;
using Composable.Logging;
using Composable.Persistence.MySql.SystemExtensions;
using Composable.Persistence.MsSql.SystemExtensions;
using Composable.System;
using Composable.Testing;
using Composable.Testing.Databases;
using Composable.Testing.Performance;
using NCrunch.Framework;
using NUnit.Framework;

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
        public void Single_thread_can_reserve_and_release_10_identically_named_databases_in_300_milliseconds()
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
                maxTotal: 300.Milliseconds());
        }

        [Test]
        public void Multiple_threads_can_reserve_and_release_10_identically_named_databases_in_70_milliseconds()
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
                timeIndividualExecutions: true,
                maxTotal: 70.Milliseconds());
        }

        [Test]
        public void Multiple_threads_can_reserve_and_release_10_differently_named_databases_in_300_milliseconds()
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
                maxTotal: 300.Milliseconds()
            );
        }

        [Test]
        public void Single_thread_can_reserve_and_release_10_differently_named_databases_in_300_milliseconds()
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
                maxTotal: 300.Milliseconds()
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
        public void Once_DB_Fetched_MsSql_Can_use_400_connections_in_10_milliseconds_MySql_40()
        {
            using var manager = CreatePool();
            manager.SetLogLevel(LogLevel.Warning);
            var reservationName = Guid.NewGuid().ToString();
            TestConnectionUsage(manager, reservationName);
        }

        static void TestConnectionUsage(DatabasePool manager, string reservationName)
        {
            switch(TestEnvironment.TestingPersistenceLayer)
            {
                case PersistenceLayer.MsSql:
                {
                    var connectionProvider = new MsSqlConnectionProvider(manager.ConnectionStringFor(reservationName));
                    connectionProvider.UseConnection(_ => {});

                    TimeAsserter.Execute(
                        action: () => connectionProvider.UseConnection(_ => {}),
                        iterations: 400,
                        maxTotal: 10.Milliseconds()
                    );
                }
                    break;
                case PersistenceLayer.InMemory:
                    break;
                case PersistenceLayer.MySql:
                {
                    var connectionProvider = new MySqlConnectionProvider(manager.ConnectionStringFor(reservationName));
                    connectionProvider.UseConnection(_ => {});

                    //Performance: do something about the performance of opening MySql connections.
                    TimeAsserter.Execute(
                        action: () => connectionProvider.UseConnection(_ => {}),
                        iterations: 40,
                        maxTotal: 10.Milliseconds()
                    );
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
