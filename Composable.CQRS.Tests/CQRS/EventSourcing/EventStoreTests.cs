using System;
using System.Configuration;
using System.Linq;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.MicrosoftSQLServer;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
using Composable.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.CQRS.Tests.CQRS.EventSourcing
{
    interface ISomeEvent : IAggregateRootEvent {}

    class SomeEvent : AggregateRootEvent, ISomeEvent
    {
        public SomeEvent(Guid aggregateRootId, int version): base(aggregateRootId)
        {
            AggregateRootVersion = version;
            UtcTimeStamp = new DateTime(UtcTimeStamp.Year, UtcTimeStamp.Month, UtcTimeStamp.Day, UtcTimeStamp.Hour, UtcTimeStamp.Minute, UtcTimeStamp.Second);
        }
    }

    [TestFixture]
    public abstract class EventStoreTests
    {
        protected abstract IEventStore CreateEventStore();
        protected abstract IEventStore CreateEventStore2();

        [Test]
        public void StreamEventsSinceReturnsWholeEventLogWhenFromEventIdIsNull()
        {
            using (var eventStore = CreateEventStore())
            {
                Guid aggregateId = Guid.NewGuid();
                eventStore.SaveEvents(1.Through(10).Select(i => new SomeEvent(aggregateId, i)));
                var stream = eventStore.ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize();

                stream.Should().HaveCount(10);
            }
        }


        [Test,Category("LongRunning")]
        public void StreamEventsSinceReturnsWholeEventLogWhenFetchingALargeNumberOfEvents_EnsureBatchingDoesNotBreakThings()
        {
            using (var eventStore = CreateEventStore())
            {
                const int moreEventsThanTheBatchSizeForStreamingEvents = SqlServerEventStore.StreamEventsBatchSize + 100;
                var aggregateId = Guid.NewGuid();
                eventStore.SaveEvents(1.Through(moreEventsThanTheBatchSizeForStreamingEvents).Select(i => new SomeEvent(aggregateId, i)));
                var stream = eventStore.ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize().ToList();

                var currentEventNumber = 0;
                stream.Should().HaveCount(moreEventsThanTheBatchSizeForStreamingEvents);
                foreach(var aggregateRootEvent in stream)
                {
                    aggregateRootEvent.AggregateRootVersion.Should().Be(++currentEventNumber, "Incorrect event version detected");
                }
            }
        }


        [Test]
        public void DeleteEventsDeletesTheEventsForOnlyTheSpecifiedAggregate()
        {
            var aggregatesWithEvents = 1.Through(10).ToDictionary(i => i,
                                                                  i =>
                                                                  {
                                                                      var aggregateId = Guid.NewGuid();
                                                                      return 1.Through(10).Select(j => new SomeEvent(aggregateId, j)).ToList();
                                                                  });

            using (var eventStore = CreateEventStore())
            {
                eventStore.SaveEvents(aggregatesWithEvents.SelectMany(x => x.Value));
                var toRemove = aggregatesWithEvents[2][0].AggregateRootId;
                aggregatesWithEvents.Remove(2);

                eventStore.DeleteEvents(toRemove);

                foreach (var kvp in aggregatesWithEvents)
                {
                    var stream = eventStore.GetAggregateHistory(kvp.Value[0].AggregateRootId);
                    stream.Should().HaveCount(10);
                }
                eventStore.GetAggregateHistory(toRemove).Should().BeEmpty();
            }
        }

        [Test]
        public void GetListOfAggregateIds()
        {
            var aggregatesWithEvents = 1.Through(10).ToDictionary(i => i,
                                                                  i =>
                                                                  {
                                                                      var aggregateId = Guid.NewGuid();
                                                                      return 1.Through(10).Select(j => new SomeEvent(aggregateId, j)).ToList();
                                                                  });

            using (var eventStore = CreateEventStore())
            {
                eventStore.SaveEvents(aggregatesWithEvents.SelectMany(x => x.Value));
                var allAggregateIds = eventStore.StreamAggregateIdsInCreationOrder().ToList();
                Assert.AreEqual(aggregatesWithEvents.Count, allAggregateIds.Count);
            }
        }

        [Test]
        public void GetListOfAggregateIdsUsingBaseEventType()
        {
            var aggregatesWithEvents = 1.Through(10).ToDictionary(i => i,
                                                                  i =>
                                                                  {
                                                                      var aggregateId = Guid.NewGuid();
                                                                      return 1.Through(10).Select(j => new SomeEvent(aggregateId, j)).ToList();
                                                                  });

            using (var eventStore = CreateEventStore())
            {
                eventStore.SaveEvents(aggregatesWithEvents.SelectMany(x => x.Value));
                var allAggregateIds = eventStore.StreamAggregateIdsInCreationOrder<ISomeEvent>().ToList();
                Assert.AreEqual(aggregatesWithEvents.Count, allAggregateIds.Count);
            }
        }
    }


    [TestFixture]
    public class InMemoryEventStoreTests : EventStoreTests
    {
        protected override IEventStore CreateEventStore() => new InMemoryEventStore();

        protected override IEventStore CreateEventStore2() => new InMemoryEventStore();
    }

    [TestFixture]
    public class SqlServerEventStoreTests : EventStoreTests
    {
        string _connectionString1;
        string _connectionString2;
        static SqlServerDatabasePool _tempDbManager;

        [SetUp]
        public void SetupFixture()
        {
            _tempDbManager = new SqlServerDatabasePool(ConfigurationManager.ConnectionStrings["MasterDb"].ConnectionString);
            _connectionString1 = _tempDbManager.ConnectionStringFor("SqlServerEventStoreTests_EventStore1");
            _connectionString2 = _tempDbManager.ConnectionStringFor("SqlServerEventStoreTests_EventStore2");

            SqlServerEventStore.ClearAllCache();
        }

        [TearDown]
        public void TearDownTask()
        {
            _tempDbManager.Dispose();
        }

        protected override IEventStore CreateEventStore() => new SqlServerEventStore(_connectionString1, new SingleThreadUseGuard());

        protected override IEventStore CreateEventStore2() => new SqlServerEventStore(_connectionString2, new SingleThreadUseGuard());

        [Test]
        public void DoesNotMixUpEventsFromDifferentStores()
        {
            //Two histories with the same in different event stores.
            var aggregateId = Guid.NewGuid();
            var aggregate1Events = 1.Through(3).Select(j => new SomeEvent(aggregateRootId: aggregateId, version: j)).ToList();
            var aggregate2Events = 1.Through(5).Select(j => new SomeEvent(aggregateRootId: aggregateId, version: j)).ToList();

            var store1Events = Seq.Empty<IAggregateRootEvent>();
            var store2Events = Seq.Empty<IAggregateRootEvent>();
            var store1NewStoreEvents = Seq.Empty<IAggregateRootEvent>();
            var store2NewStoreEvents = Seq.Empty<IAggregateRootEvent>();


            try
            {
                using (var store1 = CreateEventStore())
                using(var store2 = CreateEventStore2())
                {
                    store1.SaveEvents(aggregate1Events);
                    store2.SaveEvents(aggregate2Events);

                    store1Events = store1.GetAggregateHistory(aggregateId);
                    store2Events = store2.GetAggregateHistory(aggregateId);
                }

                using(var store1 = CreateEventStore())
                using(var store2 = CreateEventStore2())
                {
                    store1NewStoreEvents = store1.GetAggregateHistory(aggregateId);
                    store2NewStoreEvents = store2.GetAggregateHistory(aggregateId);
                }

                store1Events.ShouldAllBeEquivalentTo(aggregate1Events, config  => config.WithStrictOrdering());
                store2Events.ShouldAllBeEquivalentTo(aggregate2Events, config => config.WithStrictOrdering());
                store1NewStoreEvents.ShouldAllBeEquivalentTo(aggregate1Events, config => config.WithStrictOrdering());
                store2NewStoreEvents.ShouldAllBeEquivalentTo(aggregate2Events, config => config.WithStrictOrdering());

            }
            catch(Exception)
            {
                Console.WriteLine("aggregate1 events");
                aggregate1Events.ForEach(e => Console.WriteLine($"   {e}"));

                Console.WriteLine("\n\naggregate2 events");
                aggregate2Events.ForEach(e => Console.WriteLine($"   {e}"));

                Console.WriteLine("\n\nloaded events from eventstore 1");
                store1Events.ForEach(e => Console.WriteLine($"   {e}"));

                Console.WriteLine("\n\nloaded events from eventstore 2");
                store2Events.ForEach(e => Console.WriteLine($"   {e}"));


                Console.WriteLine("\n\nloaded events from new eventstore 1");
                store1NewStoreEvents.ForEach(e => Console.WriteLine($"   {e}"));

                Console.WriteLine("\n\nloaded events from new eventstore 2");
                store2NewStoreEvents.ForEach(e => Console.WriteLine($"   {e}"));

                throw;
            }
        }
    }

}