using System;
using System.Configuration;
using Composable.Logging;
using Composable.System.Data.SqlClient;
using Composable.Testing;
using NUnit.Framework;
using Composable.System;
using Composable.Testing.Databases;
using Composable.Testing.Performance;

namespace Composable.CQRS.Tests.SqlServerDatabasePoolTests
{
    [TestFixture, Performance]
    public class PerformanceTests
    {
        static readonly string MasterConnectionString = ConfigurationManager.ConnectionStrings["MasterDB"].ConnectionString;

        [SetUp]
        public void WarmUpCache()
        {
            using(new SqlServerDatabasePool(MasterConnectionString)) { }
        }

        [Test]
        public void Single_thread_can_reserve_and_release_10_identically_named_databases_in_30_milliseconds()
        {
            var dbName = "74EA37DF-03CE-49C4-BDEC-EAD40FAFB3A1";

            TimeAsserter.Execute(
                action:
                () =>
                {
                    using(var manager = new SqlServerDatabasePool(MasterConnectionString))
                    {
                        manager.SetLogLevel(LogLevel.Warning);
                        manager.ConnectionProviderFor(dbName).UseConnection(_ => {});
                    }
                },
                iterations: 10,
                maxTotal: 30.Milliseconds());
        }

        [Test]
        public void Multiple_threads_can_reserve_and_release_10_identically_named_databases_in_50_milliseconds()
        {
            var dbName = "EB82270F-E0BA-49F7-BC09-79AE95BA109F";

            TimeAsserter.ExecuteThreaded(
                action:
                () =>
                {
                    using(var manager = new SqlServerDatabasePool(MasterConnectionString))
                    {
                        manager.SetLogLevel(LogLevel.Warning);
                        manager.ConnectionProviderFor(dbName).UseConnection(_ => { });
                    }
                },
                iterations: 10,
                timeIndividualExecutions: true,
                maxTotal: 50.Milliseconds());
        }

        [Test]
        public void Multiple_threads_can_reserve_and_release_10_differently_named_databases_in_20_milliseconds()
        {
            SqlServerDatabasePool manager = null;

            TimeAsserter.ExecuteThreaded(
                setup: () =>
                       {
                           manager = new SqlServerDatabasePool(MasterConnectionString);
                           manager.SetLogLevel(LogLevel.Warning);
                           manager.ConnectionProviderFor("fake_to_force_creation_of_manager_database").UseConnection(_ => { });
                       },
                tearDown: () => manager.Dispose(),
                action: () => manager.ConnectionProviderFor(Guid.NewGuid().ToString()).UseConnection(_ => { }),
                iterations: 10,
                maxTotal: 20.Milliseconds()
            );
        }

        [Test]
        public void Single_thread_can_reserve_and_release_10_differently_named_databases_in_15_milliseconds()
        {
            SqlServerDatabasePool manager = null;

            TimeAsserter.Execute(
                setup: () =>
                       {
                           manager = new SqlServerDatabasePool(MasterConnectionString);
                           manager.SetLogLevel(LogLevel.Warning);
                           manager.ConnectionProviderFor("fake_to_force_creation_of_manager_database").UseConnection(_ => { });
                       },
                tearDown: () => manager.Dispose(),
                action: () => manager.ConnectionProviderFor(Guid.NewGuid().ToString()).UseConnection(_ => { }),
                iterations: 10,
                maxTotal: 15.Milliseconds()
            );
        }

        [Test]
        public void Repeated_fetching_of_same_connection_runs_200_times_in_ten_milliseconds()
        {
            var dbName = "4669B59A-E0AC-4E76-891C-7A2369AE0F2F";
            using(var manager = new SqlServerDatabasePool(MasterConnectionString))
            {
                manager.SetLogLevel(LogLevel.Warning);
                manager.ConnectionProviderFor(dbName).UseConnection(_ => { });

                TimeAsserter.Execute(
                    action: () => manager.ConnectionProviderFor(dbName).UseConnection(_ => { }),
                    iterations: 200,
                    maxTotal: 10.Milliseconds()
                );
            }
        }
    }
}
