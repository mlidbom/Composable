using System;
using System.Configuration;
using Composable.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.CQRS.Tests.SqlServerDatabasePoolTests
{
    [TestFixture, Performance]
    public class PerformanceTests
    {
        static readonly string MasterConnectionString = ConfigurationManager.ConnectionStrings["MasterDB"].ConnectionString;

        [OneTimeSetUp] public void ResetDatabases()
        {
            //SqlServerDatabasePool.DropAllAndStartOver(MasterConnectionString);
        }

        [SetUp]
        public void WarmUpCache()
        {
            using(new SqlServerDatabasePool(MasterConnectionString)) { }
        }

        [Test]
        public void Single_thread_can_reserve_and_release_10_identically_named_databases_in_500_milliseconds()
        {
            var dbName = "74EA37DF-03CE-49C4-BDEC-EAD40FAFB3A1";

            TimeAsserter.Execute(
                action:
                () =>
                {
                    using(var manager = new SqlServerDatabasePool(MasterConnectionString))
                    {
                        manager.ConnectionStringFor(dbName);
                    }
                },
                iterations: 10,
                maxTotal: 500.Milliseconds(),
                maxTries: 10);
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
                        manager.ConnectionStringFor(dbName);
                    }
                },
                iterations: 10,
                timeIndividualExecutions: true,
                maxTotal: 50.Milliseconds(),
                maxTries: 10);
        }

        [Test]
        public void Multiple_threads_can_reserve_and_release_10_differently_named_databases_in_60_milliseconds()
        {
            SqlServerDatabasePool manager = null;


            TimeAsserter.ExecuteThreaded(
                setup: () =>
                       {
                           manager = new SqlServerDatabasePool(MasterConnectionString);
                           manager.ConnectionStringFor("fake_to_force_creation_of_manager_database");
                       },
                tearDown: () => manager.Dispose(),
                action: () => manager.ConnectionStringFor(Guid.NewGuid()
                                                              .ToString()),
                iterations: 10,
                maxTries: 10,
                maxTotal: 60.Milliseconds()
            );
        }

        [Test]
        public void Single_thread_can_reserve_and_release_10_differently_named_databases_in_400_milliseconds()
        {
            SqlServerDatabasePool manager = null;

            TimeAsserter.Execute(
                setup: () =>
                       {
                           manager = new SqlServerDatabasePool(MasterConnectionString);
                           manager.ConnectionStringFor("fake_to_force_creation_of_manager_database");
                       },
                tearDown: () => manager.Dispose(),
                action: () => manager.ConnectionStringFor(Guid.NewGuid()
                                                              .ToString()),
                iterations: 10,
                maxTries: 5,
                maxTotal: 400.Milliseconds()
            );
        }

        [Test]
        public void Repeated_fetching_of_same_connection_runs_1000_times_in_ten_milliseconds()
        {
            var dbName = "4669B59A-E0AC-4E76-891C-7A2369AE0F2F";
            using(var manager = new SqlServerDatabasePool(MasterConnectionString))
            {
                manager.ConnectionStringFor(dbName);

                TimeAsserter.Execute(
                    action: () => manager.ConnectionStringFor(dbName),
                    iterations: 1000,
                    maxTotal: 10.Milliseconds()
                );
            }
        }
    }
}
