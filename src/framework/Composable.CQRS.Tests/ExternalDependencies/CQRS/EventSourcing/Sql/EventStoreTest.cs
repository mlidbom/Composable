﻿using System;
using System.Linq;
using System.Transactions;
using Composable.DependencyInjection;
using Composable.Persistence.EventStore;
using Composable.Refactoring.Naming;
using Composable.Tests.CQRS;
using NUnit.Framework;

namespace Composable.Tests.ExternalDependencies.CQRS.EventSourcing.Sql
{
    //urgent: Remove this attribute once whole assembly runs all persistence layers.
    [NCrunch.Framework.DuplicateByDimensions(nameof(PersistenceLayer.SqlServer), nameof(PersistenceLayer.InMemory))]
    [TestFixture]
    public class EventStoreTest
    {
        IServiceLocator _serviceLocator;
        [SetUp] public void SetupTask()
        {
            _serviceLocator = TestWiringHelper.SetupTestingServiceLocator();
            _serviceLocator.Resolve<ITypeMappingRegistar>()
                           .Map<Composable.Tests.CQRS.UserRegistered>("e965b5d4-6f1a-45fa-9660-2fec0abc4a0a");
        }

        [TearDown] public void TearDownTask()
        {
            _serviceLocator.Dispose();
        }

        [Test]
        public void Does_not_call_db_in_constructor()
        {
                _serviceLocator.ExecuteInIsolatedScope(() => _serviceLocator.Resolve<IEventStoreUpdater>());
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
                                                           eventStore.SaveSingleAggregateEvents(((IEventStored)user).GetChanges());
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

                using var tran = new TransactionScope();
                eventStore.SaveSingleAggregateEvents(stored.GetChanges());
                eventStore.GetAggregateHistory(user.Id);
                Assert.That(eventStore.GetAggregateHistory(user.Id), Is.Not.Empty);
                tran.Complete();
            }

            IAggregateEvent firstRead;
            using(_serviceLocator.BeginScope())
            {
                firstRead = _serviceLocator.SqlEventStore().GetAggregateHistory(user.Id).Single();
            }

            IAggregateEvent secondRead;
            using (_serviceLocator.BeginScope())
            {
                secondRead = _serviceLocator.SqlEventStore().GetAggregateHistory(user.Id).Single();
            }

            Assert.That(firstRead, Is.SameAs(secondRead));
        }
    }
}