using System;
using Composable.Logging;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Serialization.NewtonSoft;
using Composable.System.Diagnostics;
using Composable.Testing;
using FluentAssertions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Composable.CQRS.Tests.NewtonSoft
{
    [TestFixture, Performance]
    public class SqlServerEventStoreSerializerTests
    {
        readonly IEventStoreEventSerializer _eventSerializer = new NewtonSoftEventStoreEventSerializer();

        class TestEvent : AggregateRootEvent
        {
            [UsedImplicitly]
            public TestEvent() { }

            public TestEvent(string test1, string test2)
            {
                Test1 = test1;
                Test2 = test2;
            }

            public TestEvent(
                string test1,
                string test2,
                Guid eventId,
                int aggregateRootVersion,
                Guid aggregateRootId,
                long insertionOrder,
                long? replaces,
                long? insertBefore,
                long? insertAfter,
                DateTime utcTimeStamp)
#pragma warning disable CS0618 // Type or member is obsolete
                : base(
                       aggregateRootId: aggregateRootId,
                       aggregateRootVersion: aggregateRootVersion,
                       eventId: eventId,
                       insertAfter: insertAfter,
                       insertBefore: insertBefore,
                       replaces: replaces,
                       insertionOrder: insertionOrder,
                       utcTimeStamp: utcTimeStamp
                      )
#pragma warning restore CS0618 // Type or member is obsolete
            {
                Test1 = test1;
                Test2 = test2;
            }

            // ReSharper disable once MemberCanBePrivate.Local
            public string Test1 { [UsedImplicitly] get; private set; }
            // ReSharper disable once MemberCanBePrivate.Local
            public string Test2 { [UsedImplicitly] get; private set; }
        }


        [Test]
        public void IgnoresAllIAggregateRootEventProperties()
        {
            var eventWithAllValuesSet = new TestEvent(
                                            test1: "Test1",
                                            test2: "Test2",
                                            aggregateRootId:  Guid.NewGuid(),
                                            aggregateRootVersion:  2,
                                            eventId: Guid.NewGuid(),
                                            insertAfter:  10,
                                            insertBefore:  20,
                                            replaces: 30,
                                            insertionOrder: 40,
                                            utcTimeStamp: DateTime.Now + 1.Minutes());

            var eventWithOnlySubclassValues = new TestEvent("Test1", "Test2")
                                              {
                                                  EventId = Guid.Empty,
                                                  UtcTimeStamp = DateTime.MinValue
                                              };

            var eventWithAllValuesJson = _eventSerializer.Serialize(eventWithAllValuesSet);
            var eventWithOnlySubclassValuesJson = _eventSerializer.Serialize(eventWithOnlySubclassValues);
            var roundTripped = _eventSerializer.Deserialize(typeof(TestEvent), eventWithAllValuesJson);

            SafeConsole.WriteLine(eventWithAllValuesJson);

            eventWithAllValuesJson.Should().Be(@"{
  ""Test1"": ""Test1"",
  ""Test2"": ""Test2""
}");
            eventWithAllValuesJson.Should().Be(eventWithOnlySubclassValuesJson);

            roundTripped.ShouldBeEquivalentTo(eventWithOnlySubclassValues,
                config => config.Excluding(@event => @event.UtcTimeStamp)//Timestamp is defaulted in the constructor used by serialization.
                        .Excluding(@event => @event.EventId)
                );
        }

        [Test] public void Should_roundtrip_simple_event_10000_times_in_100_milliseconds_with_new_instance_for_each_serialization()
        {
            var @event = new TestEvent(
                                            test1: "Test1",
                                            test2: "Test2",
                                            aggregateRootId: Guid.NewGuid(),
                                            aggregateRootVersion: 2,
                                            eventId: Guid.NewGuid(),
                                            insertAfter: 10,
                                            insertBefore: 20,
                                            replaces: 30,
                                            insertionOrder: 40,
                                            utcTimeStamp: DateTime.Now + 1.Minutes());
            TimeAsserter.Execute(
                                 () =>
                                 {
                                     var eventJson = _eventSerializer.Serialize(@event);
                                     _eventSerializer.Deserialize(typeof(TestEvent), eventJson);
                                 },
                                 iterations:10000,
                                 maxTotal: 100.Milliseconds().AdjustRuntimeToTestEnvironment()
                                );
        }

        [Test] public void Should_roundtrip_simple_event_within_20_percent_of_default_serializer_performance_given_all_new_serializer_instances()
        {
            const int iterations = 1000;
            const double allowedSlowdown = 1.2;

            var @event = new TestEvent(
                                            test1: "Test1",
                                            test2: "Test2",
                                            aggregateRootId: Guid.NewGuid(),
                                            aggregateRootVersion: 2,
                                            eventId: Guid.NewGuid(),
                                            insertAfter: 10,
                                            insertBefore: 20,
                                            replaces: 30,
                                            insertionOrder: 40,
                                            utcTimeStamp: DateTime.Now + 1.Minutes());

            _eventSerializer.Serialize(@event);//Warmup

            var settings = NewtonSoftEventStoreEventSerializer.JsonSettings;
            var defaultSerializerPerformanceNumbers = StopwatchExtensions.TimeExecution(() =>
                                                                                        {
                                                                                            var eventJson = JsonConvert.SerializeObject(@event, settings);
                                                                                            JsonConvert.DeserializeObject<TestEvent>(eventJson, settings);
                                                                                        },

                                                                                        iterations: iterations);

            var allowedTime = TimeSpan.FromMilliseconds(defaultSerializerPerformanceNumbers.Total.TotalMilliseconds * allowedSlowdown);

            TimeAsserter.Execute(() =>
                                 {
                                     var eventJson = _eventSerializer.Serialize(@event);
                                     _eventSerializer.Deserialize(typeof(TestEvent), eventJson);
                                 },
                                 iterations: iterations,
                                 maxTotal: allowedTime);
        }
    }
}