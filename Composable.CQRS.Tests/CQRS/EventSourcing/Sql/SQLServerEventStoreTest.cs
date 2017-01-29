#region usings

using System;
using System.Configuration;
using System.Transactions;
using Composable.CQRS.EventSourcing;
using NUnit.Framework;
using System.Linq;
using Composable.CQRS.EventSourcing.MicrosoftSQLServer;
using Composable.CQRS.Testing;
using Composable.SystemExtensions.Threading;

#endregion

namespace CQRS.Tests.CQRS.EventSourcing.Sql
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
                var something = new SqlServerEventStore(connectionString, new SingleThreadUseGuard());
                something.ResetDB(); //Sometimes the test would fail on the last line with information that the table was missing. Probably because the table was created during the aborted transaction. I'm hoping this will fix it.

                var user = new User();
                user.Register("email@email.se", "password", Guid.NewGuid());

                using(new TransactionScope())
                {
                    something.SaveEvents(((IEventStored)user).GetChanges());
                    something.GetAggregateHistory(user.Id);
                    Assert.That(something.GetAggregateHistory(user.Id), Is.Not.Empty);
                }

                Assert.That(something.GetAggregateHistory(user.Id), Is.Empty);
            }
        }

        [Test]
        public void ShouldCacheEventsBetweenInstancesTransaction()
        {
            using(var dbManager = new SqlServerDatabasePool(ConfigurationManager.ConnectionStrings["MasterDb"].ConnectionString))
            {
                var connectionString = dbManager.ConnectionStringFor("SqlServerEventStoreTest_EventStore2");
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