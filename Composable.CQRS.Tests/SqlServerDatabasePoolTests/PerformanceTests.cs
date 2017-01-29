using System;
using System.Configuration;
using System.Threading;
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

        [Test]
        public void Single_thread_can_reserve_and_release_100_identically_named_databases_in_5_seconds()
        {
            var dbName = "EB82270F-E0BA-49F7-BC09-79AE95BA109F";

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
                maxTotal: 5.Seconds());
        }

        [Test]
        public void Multiple_threads_can_reserve_and_release_100_identically_named_databases_in_3_seconds()
        {
            var dbName = "EB82270F-E0BA-49F7-BC09-79AE95BA109F";

            TimeAsserter.ExecuteThreaded(
                action:
                () =>
                {
                    using (var manager = new SqlServerDatabasePool(_masterConnectionString))
                    {
                        var connection1 = manager.ConnectionStringFor(dbName);
                    }
                },
                iterations: 100,
                timeIndividualExecutions:true,
                maxTotal: 3.Seconds());
        }
    }
}