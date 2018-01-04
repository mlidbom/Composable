using System;
using System.Collections.Generic;
using Composable.GenericAbstractions.Time;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.AggregateRoots;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.CQRS
{
    using Composable.System.Reactive;

    [TestFixture]
    public class AggregateRootTests
    {
        [Test]
        public void VersionIncreasesWithEachAppliedEvent()
        {
            var user = new User();
            Assert.That(user.Version, Is.EqualTo(0));

            user.Register("email", "password", Guid.NewGuid());
            Assert.That(user.Version, Is.EqualTo(1));

            user.ChangeEmail("NewEmail");
            Assert.That(user.Version, Is.EqualTo(2));

            user.ChangePassword("NewPassword");
            Assert.That(user.Version, Is.EqualTo(3));

        }

        [Test]
        public void GetChangesReturnsEmptyListAfterAcceptChangesCalled()
        {
            var user = new User();
            var userAseventStored = (IEventStored)user;
            Assert.That(user.Version, Is.EqualTo(0));

            user.Register("email", "password", Guid.NewGuid());
            userAseventStored.AcceptChanges();
            Assert.That(userAseventStored.GetChanges(), Is.Empty);

            user.ChangeEmail("NewEmail");
            userAseventStored.AcceptChanges();
            Assert.That(userAseventStored.GetChanges(), Is.Empty);

            user.ChangePassword("NewPassword");
            userAseventStored.AcceptChanges();
            Assert.That(userAseventStored.GetChanges(), Is.Empty);
        }




        [Test]
        public void When_Raising_event_that_triggers_another_event_both_events_are_outputted_on_the_observable_only_after_the_triggered_event_and_in_the_raised_order()
        {
            var aggregate = new CascadingEventsAggregate();
            var receivedEvents = new List<IAggregateRootEvent>();
            using(((IEventStored)aggregate).EventStream.Subscribe(@event =>
                                                  {
                                                      receivedEvents.Add(@event);
                                                      aggregate.TriggeringEventApplied.Should()
                                                               .BeTrue();
                                                      aggregate.TriggeredEventApplied.Should()
                                                               .BeTrue();
                                                  }))
            {
                aggregate.RaiseTriggeringEvent();
            }

            receivedEvents.Count.Should().Be(2);
            receivedEvents[0].GetType().Should().Be(typeof(TriggeringEvent));
            receivedEvents[1].GetType().Should().Be(typeof(TriggeredEvent));
        }

        class CascadingEventsAggregate : AggregateRoot<CascadingEventsAggregate, AggregateRootEvent, IAggregateRootEvent>
        {
            public CascadingEventsAggregate():base(DummyTimeSource.Now)
            {
                RegisterEventHandlers()
                    .For<TriggeringEvent>(@event => Publish(new TriggeredEvent()));

                RegisterEventAppliers()
                    .For<TriggeringEvent>(@event => TriggeringEventApplied = true)
                    .For<TriggeredEvent>(@event => TriggeredEventApplied = true);
            }
            public bool TriggeredEventApplied { get; private set; }
            public bool TriggeringEventApplied { get; private set; }
            public void RaiseTriggeringEvent()
            {
                Publish(new TriggeringEvent());
            }
        }

        [TypeId("116B5753-FA3F-4E90-AA2F-1E0F54407DC1")]class TriggeringEvent : AggregateRootEvent, IAggregateRootCreatedEvent
        {
            public TriggeringEvent() : base(Guid.NewGuid()) {}
        }

        [TypeId("30206FD8-AA2C-48EC-B31E-50D2C648F0CF")]class TriggeredEvent : AggregateRootEvent
        {
        }
    }
}