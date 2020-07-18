using System;
using System.Linq;
using Composable.DependencyInjection;
using Composable.Logging;
using Composable.Messaging.Buses;
using Composable.Persistence.EventStore;
using Composable.Persistence.MsSql.Messaging.Buses;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.System;
using Composable.System.Diagnostics;
using Composable.System.Linq;
using Composable.Testing;
using Composable.Testing.Performance;
using FluentAssertions;
using JetBrains.Annotations;
using NCrunch.Framework;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Composable.Tests.Serialization
{
    using Composable.System;

    [TestFixture]
    public class NewtonSoftEventStoreEventSerializerTests
    {
        IEventStoreSerializer _eventSerializer;

        [OneTimeSetUp] public void SetupTask() => _eventSerializer = new EventStoreSerializer(new TypeMapper());

        class TestEvent : AggregateEvent
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
                int aggregateVersion,
                Guid aggregateId,
                DateTime utcTimeStamp):base(aggregateId)
            {
                Test1 = test1;
                Test2 = test2;

                AggregateVersion = aggregateVersion;
                UtcTimeStamp = utcTimeStamp;
            }

            // ReSharper disable once MemberCanBePrivate.Local
            public string Test1 { [UsedImplicitly] get; private set; }
            // ReSharper disable once MemberCanBePrivate.Local
            public string Test2 { [UsedImplicitly] get; private set; }
        }


        [Test]
        public void IgnoresAllIAggregateEventProperties()
        {
            var eventWithAllValuesSet = new TestEvent(
                                            test1: "Test1",
                                            test2: "Test2",
                                            aggregateId:  Guid.NewGuid(),
                                            aggregateVersion:  2,
                                            utcTimeStamp: DateTime.Now + 1.Minutes());

            TestEvent eventWithOnlySubclassValues = new TestEvent("Test1", "Test2")
                                              {
                                                  UtcTimeStamp = DateTime.MinValue
                                              };

            var eventWithAllValuesJson = _eventSerializer.Serialize(eventWithAllValuesSet);
            var eventWithOnlySubclassValuesJson = _eventSerializer.Serialize(eventWithOnlySubclassValues);
            TestEvent roundTripped = (TestEvent)_eventSerializer.Deserialize(typeof(TestEvent), eventWithAllValuesJson);

            SafeConsole.WriteLine(eventWithAllValuesJson);

            eventWithAllValuesJson.Should().Be(@"{
  ""Test1"": ""Test1"",
  ""Test2"": ""Test2""
}");
            eventWithAllValuesJson.Should().Be(eventWithOnlySubclassValuesJson);

            roundTripped.Should().BeEquivalentTo(eventWithOnlySubclassValues,
                config => config
                        .RespectingRuntimeTypes()
                        .ComparingByMembers<AggregateEvent>()
                        .Excluding(@event => @event.UtcTimeStamp)//Timestamp is defaulted in the constructor used by serialization.
                        .Excluding(@event => @event.EventId)
            );
        }

        [Test, Performance, Serial] public void Should_roundtrip_simple_event_1000_times_in_15_milliseconds()
        {
            var @event = new TestEvent(
                                            test1: "Test1",
                                            test2: "Test2",
                                            aggregateId: Guid.NewGuid(),
                                            aggregateVersion: 2,
                                            utcTimeStamp: DateTime.Now + 1.Minutes());

            //Warmup
            _eventSerializer.Deserialize(typeof(TestEvent), _eventSerializer.Serialize(@event));

            TimeAsserter.Execute(
                                 () =>
                                 {
                                     var eventJson = _eventSerializer.Serialize(@event);
                                     _eventSerializer.Deserialize(typeof(TestEvent), eventJson);
                                 },
                                 iterations:1000,
                                 maxTotal: 15.Milliseconds()
                                );
        }

        [Test, Performance, Serial] public void Should_roundtrip_simple_event_within_50_percent_of_default_serializer_performance()
        {
            const int iterations = 1000;
            const double allowedSlowdown = 1.5;

            var events = 1.Through(iterations).Select( index =>  new TestEvent(
                                            test1: "Test1",
                                            test2: "Test2",
                                            aggregateId: Guid.NewGuid(),
                                            aggregateVersion: 2,
                                            utcTimeStamp: DateTime.Now + 1.Minutes())).ToList();

            var settings = EventStoreSerializer.JsonSettings;

            //Warmup
            _eventSerializer.Deserialize(typeof(TestEvent), _eventSerializer.Serialize(events.First()));
            JsonConvert.DeserializeObject<TestEvent>(JsonConvert.SerializeObject(events.First(), settings), settings);

            var defaultSerializerPerformanceNumbers = StopwatchExtensions.TimeExecution(() =>
                                                                                        {
                                                                                            var eventJson = events.Select(@this => JsonConvert.SerializeObject(@this, settings))
                                                                                                                  .ToList();
                                                                                            eventJson.ForEach(@this => JsonConvert.DeserializeObject<TestEvent>(@this, settings));
                                                                                        });

            var allowedTime = defaultSerializerPerformanceNumbers.MultiplyBy(allowedSlowdown);


            TimeAsserter.Execute(() =>
                                 {
                                     var eventJson = events.Select(_eventSerializer.Serialize)
                                                            .ToList();
                                     eventJson.ForEach(@this => _eventSerializer.Deserialize(typeof(TestEvent), @this));
                                 },
                                 maxTotal: allowedTime);
        }
    }
}