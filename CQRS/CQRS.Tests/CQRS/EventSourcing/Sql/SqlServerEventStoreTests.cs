using System.Configuration;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.SQLServer;
using NUnit.Framework;

namespace CQRS.Tests.CQRS.EventSourcing.Sql
{
    [TestFixture]
    class SqlServerEventStoreTests : EventStoreTests
    {
        private static string connectionString = ConfigurationManager.ConnectionStrings["EventStore"].ConnectionString;
        [TestFixtureSetUp]
        public static void SetupFixture()
        {
            SqlServerEventStore.ResetDB(connectionString);            
        }

        protected override IEventStore CreateStore()
        {
            return new SqlServerEventStore(connectionString);
        }
    }
}