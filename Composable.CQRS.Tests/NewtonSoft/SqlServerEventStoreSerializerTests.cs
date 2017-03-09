using System;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.MicrosoftSQLServer;
using Composable.NewtonSoft;
using Composable.System.Diagnostics;
using Composable.Testing;
using FluentAssertions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NUnit.Framework;

namespace CQRS.Tests.NewtonSoft
{
    [TestFixture]
    public class SqlServerEventStoreSerializerTests
    {
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

            public string Test1 { get; private set; }
            public string Test2 { get; private set; }
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

            var serializer = new SqlServerEvestStoreEventSerializer();

            var eventWithAllValuesJson = serializer.Serialize(eventWithAllValuesSet);
            var eventWithOnlySubclassValuesJson = serializer.Serialize(eventWithOnlySubclassValues);
            var roundTripped = serializer.Deserialize(typeof(TestEvent), eventWithAllValuesJson);

            Console.WriteLine(eventWithAllValuesJson);

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
                                     var eventJson = new SqlServerEvestStoreEventSerializer().Serialize(@event);
                                     var roundTripped = (TestEvent)new SqlServerEvestStoreEventSerializer().Deserialize(typeof(TestEvent), eventJson);
                                 },
                                 iterations:10000,
                                 maxTotal: 100.Milliseconds().AdjustRuntimeToTestEnvironment(),
                                 maxTries:3
                                );
        }

        [Test] public void Should_roundtrip_simple_event_within_20_percent_of_default_serializer_performance_given_all_new_serializer_instances()
        {
            const int iterations = 10000;
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

            new SqlServerEvestStoreEventSerializer().Serialize(@event);//Warmup

            var settings = SqlServerEvestStoreEventSerializer.JsonSettings;
            var defaultSerializerPerformanceNumbers = StopwatchExtensions.TimeExecution(() =>
                                                                                        {
                                                                                            var eventJson = JsonConvert.SerializeObject(@event, settings);
                                                                                            var roundTripped = JsonConvert.DeserializeObject<TestEvent>(eventJson, settings);
                                                                                        },

                                                                                        iterations: iterations);

            var allowedTime = TimeSpan.FromMilliseconds(defaultSerializerPerformanceNumbers.Total.TotalMilliseconds * allowedSlowdown);

            TimeAsserter.Execute(() =>
                                 {
                                     var eventJson = new SqlServerEvestStoreEventSerializer().Serialize(@event);
                                     var roundTripped = (TestEvent)new SqlServerEvestStoreEventSerializer().Deserialize(typeof(TestEvent), eventJson);
                                 },
                                 iterations: iterations,
                                 maxTotal: allowedTime,
                                 maxTries: 10);
        }
    }
}