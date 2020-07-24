using System;
using System.Linq;
using System.Transactions;
using Composable.DependencyInjection;
using Composable.Persistence.EventStore;
using Composable.Refactoring.Naming;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.TransactionsCE;
using Composable.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.CQRS
{
    interface ISomeEvent : IAggregateEvent {}

    class SomeEvent : AggregateEvent, ISomeEvent
    {
        public SomeEvent(Guid aggregateId, int version) : base(aggregateId)
        {
            AggregateVersion = version;
            UtcTimeStamp = new DateTime(UtcTimeStamp.Year, UtcTimeStamp.Month, UtcTimeStamp.Day, UtcTimeStamp.Hour, UtcTimeStamp.Minute, UtcTimeStamp.Second);
        }
    }

    //[ConfigurationBasedDuplicateByDimensions]
    public class EventStoreTests : DuplicateByPluggableComponentTest
    {
        IEventStore EventStore => _serviceLocator.EventStore();

        IServiceLocator _serviceLocator;

        [SetUp] public void SetupTask()
        {
            _serviceLocator = TestWiringHelper.SetupTestingServiceLocator();
            _serviceLocator.Resolve<ITypeMappingRegistar>()
                           .Map<SomeEvent>("9e71c8cb-397a-489c-8ff7-15805a7509e8")
                           .Map<UserRegistered>("e965b5d4-6f1a-45fa-9660-2fec0abc4a0a");
        }

        [TearDown] public void TearDownTask() { _serviceLocator.Dispose(); }

        [Test] public void StreamEventsSinceReturnsWholeEventLogWhenFromEventIdIsNull() => _serviceLocator.ExecuteInIsolatedScope(() =>
        {
            var aggregateId = Guid.NewGuid();
            TransactionScopeCe.Execute(() => EventStore.SaveSingleAggregateEvents(1.Through(10)
                                                                                    .Select(i => new SomeEvent(aggregateId, i)).ToList()));
            var stream = EventStore.ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize();

            stream.Should()
                  .HaveCount(10);
        });

        [Test] public void StreamEventsSinceReturnsWholeEventLogWhenFetchingALargeNumberOfEvents_EnsureBatchingDoesNotBreakThings() => _serviceLocator.ExecuteInIsolatedScope(() =>
        {
            const int batchSize = 100;
            const int moreEventsThanTheBatchSizeForStreamingEvents = batchSize + 10;
            var aggregateId = Guid.NewGuid();

            TransactionScopeCe.Execute(() => EventStore.SaveSingleAggregateEvents(1.Through(moreEventsThanTheBatchSizeForStreamingEvents)
                                                                                    .Select(i => new SomeEvent(aggregateId, i)).ToList()));

            var stream = EventStore.ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize(batchSize: batchSize)
                                    .ToList();

            var currentEventNumber = 0;
            stream.Should()
                  .HaveCount(moreEventsThanTheBatchSizeForStreamingEvents);
            foreach(var aggregateEvent in stream)
            {
                aggregateEvent.AggregateVersion.Should()
                              .Be(++currentEventNumber, "Incorrect event version detected");
            }
        });

        [Test] public void DeleteEventsDeletesTheEventsForOnlyTheSpecifiedAggregate() => _serviceLocator.ExecuteInIsolatedScope(() =>
        {
            var aggregatesWithEvents = 1.Through(10)
                                        .ToDictionary(i => i,
                                                      i =>
                                                      {
                                                          var aggregateId = Guid.NewGuid();
                                                          return 1.Through(10)
                                                                  .Select(j => new SomeEvent(aggregateId, j))
                                                                  .ToList();
                                                      });

            TransactionScopeCe.Execute(() => aggregatesWithEvents.ForEach(@this => EventStore.SaveSingleAggregateEvents(@this.Value)));
            var toRemove = aggregatesWithEvents[2][0]
               .AggregateId;
            aggregatesWithEvents.Remove(2);

            TransactionScopeCe.Execute(() => EventStore.DeleteAggregate(toRemove));

            foreach(var kvp in aggregatesWithEvents)
            {
                var stream = EventStore.GetAggregateHistory(kvp.Value[0]
                                                                .AggregateId);
                stream.Should()
                      .HaveCount(10);
            }

            EventStore.GetAggregateHistory(toRemove)
                       .Should()
                       .BeEmpty();
        });

        [Test] public void GetListOfAggregateIds() => _serviceLocator.ExecuteInIsolatedScope(() =>
        {
            var aggregatesWithEvents = 1.Through(10)
                                        .ToDictionary(i => i,
                                                      i =>
                                                      {
                                                          var aggregateId = Guid.NewGuid();
                                                          return 1.Through(10)
                                                                  .Select(j => new SomeEvent(aggregateId, j))
                                                                  .ToList();
                                                      });

            TransactionScopeCe.Execute(() => aggregatesWithEvents.ForEach(@this => EventStore.SaveSingleAggregateEvents(@this.Value)));

            var allAggregateIds = EventStore.StreamAggregateIdsInCreationOrder()
                                             .ToList();
            Assert.AreEqual(aggregatesWithEvents.Count, allAggregateIds.Count);
        });

        //Todo: This does not check that only aggregates of the correct type are returned since there are only events of type SomeEvent in the store..
        [Test] public void GetListOfAggregateIdsUsingEventType() => _serviceLocator.ExecuteInIsolatedScope(() =>
        {
            var aggregatesWithEvents = 1.Through(10)
                                        .ToDictionary(i => i,
                                                      i =>
                                                      {
                                                          var aggregateId = Guid.NewGuid();
                                                          return 1.Through(10)
                                                                  .Select(j => new SomeEvent(aggregateId, j))
                                                                  .ToList();
                                                      });

            TransactionScopeCe.Execute(() => aggregatesWithEvents.ForEach(@this => EventStore.SaveSingleAggregateEvents(@this.Value)));
            var allAggregateIds = EventStore.StreamAggregateIdsInCreationOrder<ISomeEvent>()
                                             .ToList();
            Assert.AreEqual(aggregatesWithEvents.Count, allAggregateIds.Count);
        });

        [Test]
        public void Does_not_call_db_in_constructor() =>
            _serviceLocator.ExecuteInIsolatedScope(() => _serviceLocator.Resolve<IEventStoreUpdater>());

        [Test]
        public void ShouldNotCacheEventsSavedDuringFailedTransactionEvenIfReadDuringSameTransaction()
        {
            _serviceLocator.ExecuteInIsolatedScope(() =>
                                                   {
                                                       var eventStore = _serviceLocator.EventStore();

                                                       eventStore.GetAggregateHistory(Guid.NewGuid()); //Trick store into ensuring the schema exists.

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
                var eventStore = _serviceLocator.EventStore();

                user.Register("email@email.se", "password", Guid.NewGuid());
                var stored = (IEventStored)user;

                TransactionScopeCe.Execute(() =>
                {
                    eventStore.SaveSingleAggregateEvents(stored.GetChanges());
                    eventStore.GetAggregateHistory(user.Id);
                    Assert.That(eventStore.GetAggregateHistory(user.Id), Is.Not.Empty);
                });
            }

            IAggregateEvent firstRead = _serviceLocator.ExecuteInIsolatedScope(() => _serviceLocator.EventStore().GetAggregateHistory(user.Id).Single());

            IAggregateEvent secondRead = _serviceLocator.ExecuteInIsolatedScope(() =>  _serviceLocator.EventStore().GetAggregateHistory(user.Id).Single());

            Assert.That(firstRead, Is.SameAs(secondRead));
        }
        public EventStoreTests(string _) : base(_) {}
    }
}
