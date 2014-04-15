using System;
using System.Configuration;
using System.Threading;
using Castle.Windsor;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.SQLServer;
using Composable.CQRS.Testing;
using Composable.System;
using CQRS.Tests.KeyValueStorage.Sql;
using FluentAssertions;
using NCrunch.Framework;
using NUnit.Framework;
using Composable.System.Linq;
using System.Linq;

namespace CQRS.Tests.CQRS.EventSourcing
{
    public class SomeEvent : AggregateRootEvent
    {
        public SomeEvent(int aggreateRootId, int version): base(Guid.Parse("00000000-0000-0000-0000-{0:D12}".FormatWith(aggreateRootId)))
        {
            AggregateRootVersion = version;
            TimeStamp = new DateTime(TimeStamp.Year, TimeStamp.Month, TimeStamp.Day, TimeStamp.Hour, TimeStamp.Minute, TimeStamp.Second);
        }
    }

    [TestFixture]
    [ExclusivelyUses(NCrunchExlusivelyUsesResources.EventStoreDbMdf)]
    public abstract class EventStoreTests
    {
        protected abstract IEventStore CreateEventStore();
        protected abstract IEventStore CreateEventStore2();

        [Test]
        public void StreamEventsSinceReturnsWholEventLogWhenFromEventIdIsNull()
        {
            using (var somethingOrOther = CreateEventStore())
            {
                somethingOrOther.SaveEvents(1.Through(10).Select(i => new SomeEvent(1, i)));
                var stream = somethingOrOther.StreamEventsAfterEventWithId(null);

                stream.Should().HaveCount(10);
            }
        }


        [Test]
        public void StreamEventsSinceReturnsNewerEventsWhenFromEventIdIsSpecified()
        {
            var someEvents = 1.Through(10).Select(i => new SomeEvent(1, i)).ToArray();
            using (var somethingOrOther = CreateEventStore())
            {                
                somethingOrOther.SaveEvents(someEvents);
                var stream = somethingOrOther.StreamEventsAfterEventWithId(someEvents.ElementAt(4).EventId);

                stream.Should().HaveCount(5);
            }
        }

        [Test]
        public void DeleteEventsDeletesTheEventsForOnlyTheSpecifiedAggregate()
        {
            var aggregatesWithEvents = 1.Through(10).ToDictionary(i => i, i => 1.Through(10).Select(j => new SomeEvent(i, j)).ToList());

            using (var somethingOrOther = CreateEventStore())
            {                
                somethingOrOther.SaveEvents(aggregatesWithEvents.SelectMany(x => x.Value));
                var toRemove = aggregatesWithEvents[2][0].AggregateRootId;
                aggregatesWithEvents.Remove(2);

                somethingOrOther.DeleteEvents(toRemove);

                foreach (var kvp in aggregatesWithEvents)
                {
                    var stream = somethingOrOther.GetAggregateHistory(kvp.Value[0].AggregateRootId);
                    stream.Should().HaveCount(10);
                }
                somethingOrOther.GetAggregateHistory(toRemove).Should().BeEmpty();
            }
        }

        [Test]
        public void GetListOfAggregateIds()
        {
            var aggregatesWithEvents = 1.Through(10).ToDictionary(i => i, i => 1.Through(10).Select(j => new SomeEvent(i, j)).ToList());

            using (var somethingOrOther = CreateEventStore())
            {
                somethingOrOther.SaveEvents(aggregatesWithEvents.SelectMany(x => x.Value));
                var allAggregateIds = somethingOrOther.StreamAggregateIdsInCreationOrder().ToList();
                Assert.AreEqual(aggregatesWithEvents.Count, allAggregateIds.Count());
            }
        }       
    }


    [TestFixture]
    public class InMemoryEventStoreTests : EventStoreTests
    {
        protected override IEventStore CreateEventStore()
        {
            return new InMemoryEventStore();
        }

        override protected IEventStore CreateEventStore2()
        {
            return new InMemoryEventStore();
        }
    }

    [TestFixture]
    [ExclusivelyUses(NCrunchExlusivelyUsesResources.EventStoreDbMdf)]
    public class SqlServerEventStoreTests : EventStoreTests
    {
        private static readonly string ConnectionString1 = ConfigurationManager.ConnectionStrings["EventStore"].ConnectionString;
        private static readonly string ConnectionString2 = ConfigurationManager.ConnectionStrings["EventStore2"].ConnectionString;
        [SetUp]
        public static void SetupFixture()
        {
            SqlServerEventStore.ResetDB(ConnectionString1);
            SqlServerEventStore.ResetDB(ConnectionString2);
        }

        protected override IEventStore CreateEventStore()
        {
            return new SqlServerEventStore(ConnectionString1);
        }

        override protected IEventStore CreateEventStore2()
        {
            return new SqlServerEventStore(ConnectionString2);
        }

        [Test]
        public void DoesNotMixUpEventsFromDifferentStores()
        {
            //Two histories with the same in different event stores.
            var aggregate1Events = 1.Through(1).Select(j => new SomeEvent(aggreateRootId: 1, version: j)).ToList();
            var aggregate2Events = 1.Through(1).Select(j => new SomeEvent(aggreateRootId: 1, version: j)).ToList();
            var aggregateId = aggregate1Events.First().AggregateRootId;

            using (var store1 = CreateEventStore())
            using (var store2 = CreateEventStore2())
            {
                store1.SaveEvents(aggregate1Events);
                store2.SaveEvents(aggregate2Events);

                store1.GetAggregateHistory(aggregateId)
                    .Should().Equal(aggregate1Events);

                store2.GetAggregateHistory(aggregateId)
                    .Should().Equal(aggregate2Events);
            }

            using (var store1 = CreateEventStore())
            using (var store2 = CreateEventStore2())
            {
                store1.GetAggregateHistory(aggregateId)
                    .Should().Equal(aggregate1Events);

                store2.GetAggregateHistory(aggregateId)
                    .Should().Equal(aggregate2Events);
            }
        }
    }

}