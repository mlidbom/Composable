using System;
using System.Configuration;
using System.Linq;
using System.Transactions;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.MicrosoftSQLServer;
using Composable.Testing;
using NUnit.Framework;

namespace Composable.CQRS.Tests.CQRS.EventSourcing.Sql
{
    [TestFixture]
    public class SqlServerEventStoreTest
    {
        [Test]
        public void Does_not_call_db_in_constructor()
        {
            // ReSharper disable once ObjectCreationAsStatement
            new SqlServerEventStore("SomeStringThatDoesNotPointToARealSqlServer");
        }

        [Test]
        public void ShouldNotCacheEventsSavedDuringFailedTransactionEvenIfReadDuringSameTransaction()
        {
            using(var dbManager = new SqlServerDatabasePool(ConfigurationManager.ConnectionStrings["MasterDb"].ConnectionString))
            {
                var connectionString = dbManager.ConnectionStringFor("SqlServerEventStoreTest_EventStore1");
                var eventStore = new SqlServerEventStore(connectionString);

                eventStore.GetAggregateHistory(Guid.NewGuid());//Trick store inte ensuring the schema exists.

                var user = new User();
                user.Register("email@email.se", "password", Guid.NewGuid());

                using(new TransactionScope())
                {
                    eventStore.SaveEvents(((IEventStored)user).GetChanges());
                    eventStore.GetAggregateHistory(user.Id);
                    Assert.That(eventStore.GetAggregateHistory(user.Id), Is.Not.Empty);
                }

                Assert.That(eventStore.GetAggregateHistory(user.Id), Is.Empty);
            }
        }

        [Test]
        public void ShouldCacheEventsBetweenInstancesTransaction()
        {
            using(var dbManager = new SqlServerDatabasePool(ConfigurationManager.ConnectionStrings["MasterDb"].ConnectionString))
            {
                var connectionString = dbManager.ConnectionStringFor("SqlServerEventStoreTest_EventStore2");
                var eventStore = new SqlServerEventStore(connectionString);

                var user = new User();
                user.Register("email@email.se", "password", Guid.NewGuid());
                var stored = (IEventStored)user;

                using(var tran = new TransactionScope())
                {
                    eventStore.SaveEvents(stored.GetChanges());
                    eventStore.GetAggregateHistory(user.Id);
                    Assert.That(eventStore.GetAggregateHistory(user.Id), Is.Not.Empty);
                    tran.Complete();
                }

                var cache = new SqlServerEventStoreEventsCache();
                eventStore = new SqlServerEventStore(connectionString, cache: cache);
                var firstRead = eventStore.GetAggregateHistory(user.Id).Single();

                eventStore = new SqlServerEventStore(connectionString, cache: cache);
                var secondRead = eventStore.GetAggregateHistory(user.Id).Single();

                Assert.That(firstRead, Is.SameAs(secondRead));
            }
        }
    }
}