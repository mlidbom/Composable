using System.Configuration;
using Composable.CQRS.Testing;
using Composable.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace CQRS.Tests.SqlServerDatabasePoolTests
{
    [TestFixture]
    public class PerformanceTests
    {
        static string _masterConnectionString = ConfigurationManager.ConnectionStrings["MasterDB"].ConnectionString;

        [SetUp]
        public void WarmUpCache()
        {
            new SqlServerDatabasePool(_masterConnectionString);
        }

        [Test]
        public void Single_thread_can_reserve_and_release_100_identically_named_databases_in_5_seconds()
        {
            var dbName = "74EA37DF-03CE-49C4-BDEC-EAD40FAFB3A1";

            TimeAsserter.Execute(
                action:
                () =>
                {
                    using(var manager = new SqlServerDatabasePool(_masterConnectionString))
                    {
                        var connection1 = manager.ConnectionStringFor(dbName);
                    }
                },
                iterations: 100,
                maxTotal: 5.Seconds(),
                maxTries: 3);
        }

        [Test]
        public void Multiple_threads_can_reserve_and_release_100_identically_named_databases_in_2_seconds()
        {
            var dbName = "EB82270F-E0BA-49F7-BC09-79AE95BA109F";

            TimeAsserter.ExecuteThreaded(
                action:
                () =>
                {
                    using(var manager = new SqlServerDatabasePool(_masterConnectionString))
                    {
                        var connection1 = manager.ConnectionStringFor(dbName);
                    }
                },
                iterations: 100,
                timeIndividualExecutions: true,
                maxTotal: 2.Seconds(),
                maxTries:3);
        }

        [Test]
        public void Repeated_fetching_of_same_connection_runs_100_times_in_one_second()
        {
            var dbName = "4669B59A-E0AC-4E76-891C-7A2369AE0F2F";
            using (var manager = new SqlServerDatabasePool(_masterConnectionString))
            {
                var connection1 = manager.ConnectionStringFor(dbName);

                TimeAsserter.Execute(
                    action: () => manager.ConnectionStringFor(dbName),
                    iterations: 100,
                    maxTotal: 1.Seconds()
                    );
            }
        }
    }
}
