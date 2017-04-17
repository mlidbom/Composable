using System;
using System.Linq;
using Composable.DependencyInjection;
using Composable.Persistence.EventStore;
using Composable.System.Linq;
using Composable.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.CQRS.Tests.CQRS.EventSourcing
{
    interface ISomeEvent : IAggregateRootEvent {}

    class SomeEvent : AggregateRootEvent, ISomeEvent
    {
        public SomeEvent(Guid aggregateRootId, int version) : base(aggregateRootId)
        {
            AggregateRootVersion = version;
            UtcTimeStamp = new DateTime(UtcTimeStamp.Year, UtcTimeStamp.Month, UtcTimeStamp.Day, UtcTimeStamp.Hour, UtcTimeStamp.Minute, UtcTimeStamp.Second);
        }
    }

    [TestFixture] public abstract class EventStoreTests
    {
        IDisposable _scope;
        protected abstract IServiceLocator CreateServiceLocator();

        IEventStore _eventStore;

        IServiceLocator _serviceLocator;

        [SetUp] public void SetupTask()
        {
            _serviceLocator = CreateServiceLocator();
            _scope = _serviceLocator.BeginScope();
            _eventStore = _serviceLocator.EventStore();
        }

        [TearDown] public void TearDownTask()
        {
            _scope.Dispose();
            _serviceLocator.Dispose();
        }

        [Test] public void StreamEventsSinceReturnsWholeEventLogWhenFromEventIdIsNull()
        {
            Guid aggregateId = Guid.NewGuid();
            _eventStore.SaveEvents(1.Through(10)
                                   .Select(i => new SomeEvent(aggregateId, i)));
            var stream = _eventStore.ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize();

            stream.Should()
                  .HaveCount(10);
        }

        [Test, LongRunning] public void StreamEventsSinceReturnsWholeEventLogWhenFetchingALargeNumberOfEvents_EnsureBatchingDoesNotBreakThings()
        {
            const int moreEventsThanTheBatchSizeForStreamingEvents = EventStore.StreamEventsBatchSize + 100;
            var aggregateId = Guid.NewGuid();
            _eventStore.SaveEvents(1.Through(moreEventsThanTheBatchSizeForStreamingEvents)
                                   .Select(i => new SomeEvent(aggregateId, i)));
            var stream = _eventStore.ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize()
                                   .ToList();

            var currentEventNumber = 0;
            stream.Should()
                  .HaveCount(moreEventsThanTheBatchSizeForStreamingEvents);
            foreach(var aggregateRootEvent in stream)
            {
                aggregateRootEvent.AggregateRootVersion.Should()
                                  .Be(++currentEventNumber, "Incorrect event version detected");
            }
        }

        [Test] public void DeleteEventsDeletesTheEventsForOnlyTheSpecifiedAggregate()
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

            _eventStore.SaveEvents(aggregatesWithEvents.SelectMany(x => x.Value));
            var toRemove = aggregatesWithEvents[2][0]
                .AggregateRootId;
            aggregatesWithEvents.Remove(2);

            _eventStore.DeleteAggregate(toRemove);

            foreach(var kvp in aggregatesWithEvents)
            {
                var stream = _eventStore.GetAggregateHistory(kvp.Value[0]
                                                               .AggregateRootId);
                stream.Should()
                      .HaveCount(10);
            }
            _eventStore.GetAggregateHistory(toRemove)
                      .Should()
                      .BeEmpty();
        }

        [Test] public void GetListOfAggregateIds()
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

            _eventStore.SaveEvents(aggregatesWithEvents.SelectMany(x => x.Value));
            var allAggregateIds = _eventStore.StreamAggregateIdsInCreationOrder()
                                            .ToList();
            Assert.AreEqual(aggregatesWithEvents.Count, allAggregateIds.Count);
        }

        [Test] public void GetListOfAggregateIdsUsingBaseEventType()
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

            _eventStore.SaveEvents(aggregatesWithEvents.SelectMany(x => x.Value));
            var allAggregateIds = _eventStore.StreamAggregateIdsInCreationOrder<ISomeEvent>()
                                            .ToList();
            Assert.AreEqual(aggregatesWithEvents.Count, allAggregateIds.Count);
        }
    }

    [TestFixture] public class InMemoryEventStoreTests : EventStoreTests
    {
        protected override IServiceLocator CreateServiceLocator() => TestWiringHelper.SetupTestingServiceLocator(TestingMode.InMemory);
    }

    [TestFixture] public class SqlServerEventStoreTests : EventStoreTests
    {
        protected override IServiceLocator CreateServiceLocator() => TestWiringHelper.SetupTestingServiceLocator(TestingMode.RealComponents);
    }
}
