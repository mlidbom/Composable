using System;
using System.Configuration;
using System.Threading;
using Castle.Windsor;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.SQLServer;
using Composable.CQRS.Testing;
using Composable.System;
using Composable.SystemExtensions.Threading;
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
        }
    }

    [TestFixture]
    public abstract class EventSomethingOrOtherTest
    {
        protected abstract IEventStore CreateSomethingOrOther();

        [Test]
        public void StreamEventsSinceReturnsWholEventLogWhenFromEventIdIsNull()
        {
            using (var somethingOrOther = CreateSomethingOrOther())
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
            using (var somethingOrOther = CreateSomethingOrOther())
            {                
                somethingOrOther.SaveEvents(someEvents);
                var stream = somethingOrOther.StreamEventsAfterEventWithId(someEvents.ElementAt(4).EventId);

                stream.Should().HaveCount(5);
            }
        }

        [Test]
        public void ThrowsIfUsedByMultipleThreads()
        {
            IEventStore session = null;
            var wait = new ManualResetEvent(false);
            ThreadPool.QueueUserWorkItem((state) =>
            {
                session = CreateSomethingOrOther();
                wait.Set();
            });
            wait.WaitOne();
            
            Assert.Throws<MultiThreadedUseException>(() => session.Dispose());
            Assert.Throws<MultiThreadedUseException>(() => session.GetHistoryUnSafe(Guid.NewGuid()));
            Assert.Throws<MultiThreadedUseException>(() => session.SaveEvents(null));
            Assert.Throws<MultiThreadedUseException>(() => session.StreamEventsAfterEventWithId(Guid.NewGuid()).ToList());
            Assert.Throws<MultiThreadedUseException>(() => session.DeleteEvents(Guid.NewGuid()));
            Assert.Throws<MultiThreadedUseException>(() => session.GetAggregateIds().ToList());
        }

        [Test]
        public void DeleteEventsDeletesTheEventsForOnlyTheSpecifiedAggregate()
        {
            var aggregatesWithEvents = 1.Through(10).ToDictionary(i => i, i => 1.Through(10).Select(j => new SomeEvent(i, j)).ToList());

            using (var somethingOrOther = CreateSomethingOrOther())
            {                
                somethingOrOther.SaveEvents(aggregatesWithEvents.SelectMany(x => x.Value));
                var toRemove = aggregatesWithEvents[2][0].AggregateRootId;
                aggregatesWithEvents.Remove(2);

                somethingOrOther.DeleteEvents(toRemove);

                foreach (var kvp in aggregatesWithEvents)
                {
                    var stream = somethingOrOther.GetHistoryUnSafe(kvp.Value[0].AggregateRootId);
                    stream.Should().HaveCount(10);
                }
                somethingOrOther.GetHistoryUnSafe(toRemove).Should().BeEmpty();
            }
        }

        [Test]
        public void GetListOfAggregateIds()
        {
            var aggregatesWithEvents = 1.Through(10).ToDictionary(i => i, i => 1.Through(10).Select(j => new SomeEvent(i, j)).ToList());

            using (var somethingOrOther = CreateSomethingOrOther())
            {
                somethingOrOther.SaveEvents(aggregatesWithEvents.SelectMany(x => x.Value));
                var allAggretateIds = somethingOrOther.GetAggregateIds().ToList();
                Assert.AreEqual(aggregatesWithEvents.Count, allAggretateIds.Count());
            }
        }
    }


    [TestFixture]
    public class InMemoryEventSomethingOrOtherTest : EventSomethingOrOtherTest
    {
        protected override IEventStore CreateSomethingOrOther()
        {
            return new InMemoryEventStore(new SingleThreadUseGuard());
        }
    }

    [TestFixture]
    [ExclusivelyUses(NCrunchExlusivelyUsesResources.EventStoreDbMdf)]
    public class SqlServerEventSomethingOrOtherTest : EventSomethingOrOtherTest
    {
        private static string connectionString = ConfigurationManager.ConnectionStrings["EventStore"].ConnectionString;
        [SetUp]
        public static void SetupFixture()
        {
            SqlServerEventStore.ResetDB(connectionString);
        }

        protected override IEventStore CreateSomethingOrOther()
        {
            return new SqlServerEventStore(new SingleThreadUseGuard(), connectionString);
        }
    }

}