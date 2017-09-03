using System;
using System.Linq;
using System.Transactions;
using Composable.DependencyInjection;
using Composable.Persistence.EventStore;
using NUnit.Framework;

namespace Composable.CQRS.Tests.CQRS.EventSourcing.Sql
{
    [TestFixture]
    public class SqlServerEventStoreTest
    {
        IServiceLocator _serviceLocator;
        [SetUp] public void SetupTask()
        {
            _serviceLocator = TestWiringHelper.SetupTestingServiceLocator(TestingMode.DatabasePool);
        }

        [TearDown] public void TearDownTask()
        {
            _serviceLocator.Dispose();
        }

        [Test]
        public void Does_not_call_db_in_constructor()
        {
                _serviceLocator.ExecuteInIsolatedScope(() => _serviceLocator.Resolve<ITestingEventstoreUpdater>());
        }

        [Test]
        public void ShouldNotCacheEventsSavedDuringFailedTransactionEvenIfReadDuringSameTransaction()
        {
            _serviceLocator.ExecuteInIsolatedScope(() =>
                                                   {
                                                       var eventStore = _serviceLocator.SqlEventStore();

                                                       eventStore.GetAggregateHistory(Guid.NewGuid()); //Trick store inte ensuring the schema exists.

                                                       var user = new User();
                                                       user.Register("email@email.se", "password", Guid.NewGuid());

                                                       using(new TransactionScope())
                                                       {
                                                           eventStore.SaveEvents(((IEventStored)user).GetChanges());
                                                           eventStore.GetAggregateHistory(user.Id);
                                                           Assert.That(eventStore.GetAggregateHistory(user.Id), Is.Not.Empty);
                                                       }

                                                       Assert.That(eventStore.GetAggregateHistory(user.Id), Is.Empty);
                                                   });
        }

        [Test]
        public void ShouldCacheEventsBetweenInstancesTransaction()
        {
            var user = new User();
            using(_serviceLocator.BeginScope())
            {
                var eventStore = _serviceLocator.SqlEventStore();

                user.Register("email@email.se", "password", Guid.NewGuid());
                var stored = (IEventStored)user;

                using(var tran = new TransactionScope())
                {
                    eventStore.SaveEvents(stored.GetChanges());
                    eventStore.GetAggregateHistory(user.Id);
                    Assert.That(eventStore.GetAggregateHistory(user.Id), Is.Not.Empty);
                    tran.Complete();
                }
            }

            IAggregateRootEvent firstRead;
            using(_serviceLocator.BeginScope())
            {
                firstRead = _serviceLocator.SqlEventStore().GetAggregateHistory(user.Id).Single();
            }

            IAggregateRootEvent secondRead;
            using (_serviceLocator.BeginScope())
            {
                secondRead = _serviceLocator.SqlEventStore().GetAggregateHistory(user.Id).Single();
            }

            Assert.That(firstRead, Is.SameAs(secondRead));
        }
    }
}