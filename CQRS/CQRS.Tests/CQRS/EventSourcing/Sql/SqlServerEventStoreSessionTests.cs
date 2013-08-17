using System.Configuration;
using Castle.Windsor;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.SQLServer;
using Composable.CQRS.Testing;
using CQRS.Tests.KeyValueStorage.Sql;
using NCrunch.Framework;
using NUnit.Framework;

namespace CQRS.Tests.CQRS.EventSourcing.Sql
{
    [TestFixture]
    [ExclusivelyUses(NCrunchExlusivelyUsesResources.EventStoreDbMdf)]
    class SqlServerEventStoreSessionTests : EventStoreSessionTests
    {
        private static readonly string ConnectionString = ConfigurationManager.ConnectionStrings["EventStore"].ConnectionString;
        [TestFixtureSetUp]
        public static void SetupFixture()
        {
            SqlServerEventStore.ResetDB(ConnectionString);
        }

        protected override IEventStore CreateStore()
        {
            return new SqlServerEventStore(ConnectionString);
        }
    }
}