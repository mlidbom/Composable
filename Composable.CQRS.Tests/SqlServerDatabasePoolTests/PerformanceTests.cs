using System.Configuration;
using Composable.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.CQRS.Tests.SqlServerDatabasePoolTests
{
    [TestFixture]
    public class PerformanceTests
    {
        static string _masterConnectionString = ConfigurationManager.ConnectionStrings["MasterDB"].ConnectionString;

        [SetUp]
        public void WarmUpCache()
        {
            using(new SqlServerDatabasePool(_masterConnectionString)) { }
        }

        [Test]
        public void Single_thread_can_reserve_and_release_10_identically_named_databases_in_500_milliseconds()
        {
            var dbName = "74EA37DF-03CE-49C4-BDEC-EAD40FAFB3A1";

            TimeAsserter.Execute(
                action:
                () =>
                {
                    using(var manager = new SqlServerDatabasePool(_masterConnectionString))
                    {
                        manager.ConnectionStringFor(dbName);
                    }
                },
                iterations: 10,
                maxTotal: 500.Milliseconds(),
                maxTries: 3);
        }

        [Test]
        public void Multiple_threads_can_reserve_and_release_10_identically_named_databases_in_200_milliseconds()
        {
            var dbName = "EB82270F-E0BA-49F7-BC09-79AE95BA109F";

            TimeAsserter.ExecuteThreaded(
                action:
                () =>
                {
                    using(var manager = new SqlServerDatabasePool(_masterConnectionString))
                    {
                        manager.ConnectionStringFor(dbName);
                    }
                },
                iterations: 10,
                timeIndividualExecutions: true,
                maxTotal: 200.Milliseconds(),
                maxTries: 3);
        }

        [Test]
        public void Repeated_fetching_of_same_connection_runs_1000_times_in_ten_milliseconds()
        {
            var dbName = "4669B59A-E0AC-4E76-891C-7A2369AE0F2F";
            using(var manager = new SqlServerDatabasePool(_masterConnectionString))
            {
                manager.ConnectionStringFor(dbName);

                TimeAsserter.Execute(
                    action: () => manager.ConnectionStringFor(dbName),
                    iterations: 1000,
                    maxTotal: 10.Milliseconds(),
                    timeFormat: "fff"
                );
            }
        }
    }
}
