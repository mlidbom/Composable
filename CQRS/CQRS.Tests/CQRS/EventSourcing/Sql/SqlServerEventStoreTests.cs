using System.Configuration;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.SQLServer;
using NUnit.Framework;

namespace CQRS.Tests.CQRS.EventSourcing.Sql
{
    [TestFixture]
    class SqlServerEventStoreTests : EventStoreTests
    {
        protected override IEventStore CreateStore()
        {
            return new SqlServerEventStore(ConfigurationManager.ConnectionStrings["EventStore"].ConnectionString);
        }
    }
}