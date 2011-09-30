#region usings

using System;
using System.Configuration;
using System.Transactions;
using Castle.Windsor;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.SQLServer;
using Composable.CQRS.Testing;
using NUnit.Framework;
using System.Linq;

#endregion

namespace CQRS.Tests.CQRS.EventSourcing.Sql
{
    [TestFixture]
    public class SQLServerEventSomethingOrOtherTest
    {
        [Test]
        public void ShouldNotCacheEventsSavedDuringFailedTransactionEvenIfReadDuringSameTransaction()
        {
            var something = new SqlServerEventSomethingOrOther(
                new SqlServerEventStore(
                    ConfigurationManager.ConnectionStrings["EventStore"].ConnectionString,
                    new DummyServiceBus(new WindsorContainer())));

            var user = new User();
            user.Register("email@email.se", "password", Guid.NewGuid());

            using (new TransactionScope())
            {
                something.SaveEvents(((IEventStored) user).GetChanges());
                something.GetHistoryUnSafe(user.Id);
                Assert.That(something.GetHistoryUnSafe(user.Id), Is.Not.Empty);
            }

            Assert.That(something.GetHistoryUnSafe(user.Id), Is.Empty);
        }

        [Test]
        public void ShouldCacheEventsBetweenInstancesTransaction()
        {
            var store = new SqlServerEventStore(
                ConfigurationManager.ConnectionStrings["EventStore"].ConnectionString,
                new DummyServiceBus(new WindsorContainer()));

            var something = new SqlServerEventSomethingOrOther(store);

            var user = new User();
            user.Register("email@email.se", "password", Guid.NewGuid());
            var stored = (IEventStored) user;

            using (var tran = new TransactionScope())
            {
                something.SaveEvents(stored.GetChanges());
                something.GetHistoryUnSafe(user.Id);
                Assert.That(something.GetHistoryUnSafe(user.Id), Is.Not.Empty);
                tran.Complete();
            }

            something = new SqlServerEventSomethingOrOther(store);
            var firstRead = something.GetHistoryUnSafe(user.Id).Single();

            something = new SqlServerEventSomethingOrOther(store);
            var secondRead = something.GetHistoryUnSafe(user.Id).Single();

            Assert.That(firstRead, Is.SameAs(secondRead));
        }
    }
}