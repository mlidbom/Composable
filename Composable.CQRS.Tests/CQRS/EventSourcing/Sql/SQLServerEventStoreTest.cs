#region usings

using System;
using System.Configuration;
using System.Linq;
using System.Transactions;
using Composable.CQRS.CQRS.EventSourcing;
using Composable.CQRS.CQRS.EventSourcing.MicrosoftSQLServer;
using Composable.SystemExtensions.Threading;
using Composable.Testing;
using NUnit.Framework;

#endregion

namespace Composable.CQRS.Tests.CQRS.EventSourcing.Sql
{
    [TestFixture]
    public class SqlServerEventStoreTest
    {
        [Test]
        public void Does_not_call_db_in_constructor()
        {
            var eventStore = new SqlServerEventStore("SomeStringThatDoesNotPointToARealSqlServer", new SingleThreadUseGuard());
        }

        [Test]
        public void ShouldNotCacheEventsSavedDuringFailedTransactionEvenIfReadDuringSameTransaction()
        {
            using(var dbManager = new SqlServerDatabasePool(ConfigurationManager.ConnectionStrings["MasterDb"].ConnectionString))
            {
                var connectionString = dbManager.ConnectionStringFor("SqlServerEventStoreTest_EventStore1");
                SqlServerEventStore.ClearAllCache();
                var eventStore = new SqlServerEventStore(connectionString, new SingleThreadUseGuard());

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
                SqlServerEventStore.ClearAllCache();
                var something = new SqlServerEventStore(connectionString, new SingleThreadUseGuard());

                var user = new User();
                user.Register("email@email.se", "password", Guid.NewGuid());
                var stored = (IEventStored)user;

                using(var tran = new TransactionScope())
                {
                    something.SaveEvents(stored.GetChanges());
                    something.GetAggregateHistory(user.Id);
                    Assert.That(something.GetAggregateHistory(user.Id), Is.Not.Empty);
                    tran.Complete();
                }

                something = new SqlServerEventStore(connectionString, new SingleThreadUseGuard());
                var firstRead = something.GetAggregateHistory(user.Id).Single();

                something = new SqlServerEventStore(connectionString, new SingleThreadUseGuard());
                var secondRead = something.GetAggregateHistory(user.Id).Single();

                Assert.That(firstRead, Is.SameAs(secondRead));
            }
        }
    }
}