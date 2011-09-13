using System;
using System.Configuration;
using Castle.Windsor;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.SQLServer;
using Composable.CQRS.Testing;
using Composable.System;
using FluentAssertions;
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
        protected abstract IEventSomethingOrOther CreateSomethingOrOther();

        [Test]
        public void StreamEventsSinceReturnsWholEventLogWhenFromEventIdIsNull()
        {
            using (var somethingOrOther = CreateSomethingOrOther())
            {
                somethingOrOther.SaveEvents(1.Through(10).Select(i => new SomeEvent(1, i)));
            }

            using (var somethingOrOther = CreateSomethingOrOther())
            {
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
            }

            using (var somethingOrOther = CreateSomethingOrOther())
            {
                var stream = somethingOrOther.StreamEventsAfterEventWithId(someEvents.ElementAt(4).EventId);

                stream.Should().HaveCount(5);
            }
        }


    }


    [TestFixture]
    public class InMemoryEventSomethingOrOtherTest : EventSomethingOrOtherTest
    {
        private InMemoryEventStore _store;

        [SetUp]
        public void Setup()
        {
            _store = new InMemoryEventStore(new DummyServiceBus(new WindsorContainer()));
        }

        protected override IEventSomethingOrOther CreateSomethingOrOther()
        {
            return new InMemoryEventSomethingOrOther(_store);
        }
    }

    [TestFixture]
    public class SqlServerEventSomethingOrOtherTest : EventSomethingOrOtherTest
    {
        private static string connectionString = ConfigurationManager.ConnectionStrings["EventStore"].ConnectionString;
        [SetUp]
        public static void SetupFixture()
        {
            SqlServerEventStore.ResetDB(connectionString);
        }

        protected override IEventSomethingOrOther CreateSomethingOrOther()
        {
            return new SqlServerEventSomethingOrOther(new SqlServerEventStore(connectionString, new DummyServiceBus(new WindsorContainer())));
        }
    }

}