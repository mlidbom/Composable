#region usings

using System;
using System.Configuration;
using System.Transactions;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.SQLServer;
using CQRS.Tests.KeyValueStorage.Sql;
using NCrunch.Framework;
using NUnit.Framework;
using System.Linq;

#endregion

namespace CQRS.Tests.CQRS.EventSourcing.Sql
{
    [TestFixture]
    [ExclusivelyUses(NCrunchExlusivelyUsesResources.EventStoreDbMdf)]
    public class SqlServerEventStoreTest
    {
        [Test]
        public void Does_not_call_db_in_constructor()
        {
            var eventStore = new SqlServerEventStore("SomeStringThatDoesNotPointToARealSqlServer");
        }

        [Test]
        public void ShouldNotCacheEventsSavedDuringFailedTransactionEvenIfReadDuringSameTransaction()
        {
            var something = new SqlServerEventStore(ConfigurationManager.ConnectionStrings["EventStore"].ConnectionString);

            var user = new User();
            user.Register("email@email.se", "password", Guid.NewGuid());

            using (new TransactionScope())
            {
                something.SaveEvents(((IEventStored) user).GetChanges());
                something.GetAggregateHistory(user.Id);
                Assert.That(something.GetAggregateHistory(user.Id), Is.Not.Empty);
            }

            Assert.That(something.GetAggregateHistory(user.Id), Is.Empty);
        }

        [Test]
        public void ShouldCacheEventsBetweenInstancesTransaction()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["EventStore"].ConnectionString;

            var something = new SqlServerEventStore(connectionString);

            var user = new User();
            user.Register("email@email.se", "password", Guid.NewGuid());
            var stored = (IEventStored) user;

            using (var tran = new TransactionScope())
            {
                something.SaveEvents(stored.GetChanges());
                something.GetAggregateHistory(user.Id);
                Assert.That(something.GetAggregateHistory(user.Id), Is.Not.Empty);
                tran.Complete();
            }

            something = new SqlServerEventStore(connectionString);
            var firstRead = something.GetAggregateHistory(user.Id).Single();

            something = new SqlServerEventStore(connectionString);
            var secondRead = something.GetAggregateHistory(user.Id).Single();

            Assert.That(firstRead, Is.SameAs(secondRead));
        }
    }
}