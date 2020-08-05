using System;
using System.Linq;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.SystemCE;
using Composable.SystemCE.DiagnosticsCE;
using Composable.SystemCE.LinqCE;
using Composable.Testing;
using Composable.Testing.Performance;
using NCrunch.Framework;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Composable.Tests.Serialization
{
    public class NewtonSoftEventStoreEventSerializerPerformanceTests
    {
        IEventStoreSerializer _eventSerializer;

        [OneTimeSetUp] public void SetupTask() => _eventSerializer = new EventStoreSerializer(new TypeMapper());

        [Test] public void Should_roundtrip_simple_event_1000_times_in_15_milliseconds()
        {
            var @event = new NewtonSoftEventStoreEventSerializerTests.TestEvent(
                                            test1: "Test1",
                                            test2: "Test2",
                                            aggregateId: Guid.NewGuid(),
                                            aggregateVersion: 2,
                                            utcTimeStamp: DateTime.Now + 1.Minutes());

            //Warmup
            _eventSerializer.Deserialize(typeof(NewtonSoftEventStoreEventSerializerTests.TestEvent), _eventSerializer.Serialize(@event));

            TimeAsserter.Execute(
                                 () =>
                                 {
                                     var eventJson = _eventSerializer.Serialize(@event);
                                     _eventSerializer.Deserialize(typeof(NewtonSoftEventStoreEventSerializerTests.TestEvent), eventJson);
                                 },
                                 iterations:1000,
                                 maxTotal: 15.Milliseconds()
                                );
        }

        [Test] public void Should_roundtrip_simple_event_within_50_percent_of_default_serializer_performance()
        {
            const int iterations = 1000;
            const double allowedSlowdown = 1.5;

            var events = 1.Through(iterations).Select( index =>  new NewtonSoftEventStoreEventSerializerTests.TestEvent(
                                            test1: "Test1",
                                            test2: "Test2",
                                            aggregateId: Guid.NewGuid(),
                                            aggregateVersion: 2,
                                            utcTimeStamp: DateTime.Now + 1.Minutes())).ToList();

            var settings = EventStoreSerializer.JsonSettings;

            //Warmup
            _eventSerializer.Deserialize(typeof(NewtonSoftEventStoreEventSerializerTests.TestEvent), _eventSerializer.Serialize(events.First()));
            JsonConvert.DeserializeObject<NewtonSoftEventStoreEventSerializerTests.TestEvent>(JsonConvert.SerializeObject(events.First(), settings), settings);

            var defaultSerializerPerformanceNumbers = StopwatchCE.TimeExecution(() =>
                                                                                        {
                                                                                            var eventJson = events.Select(@this => JsonConvert.SerializeObject(@this, settings))
                                                                                                                  .ToList();
                                                                                            eventJson.ForEach(@this => JsonConvert.DeserializeObject<NewtonSoftEventStoreEventSerializerTests.TestEvent>(@this, settings));
                                                                                        });

            var allowedTime = defaultSerializerPerformanceNumbers.MultiplyBy(allowedSlowdown);


            TimeAsserter.Execute(() =>
                                 {
                                     var eventJson = events.Select(_eventSerializer.Serialize)
                                                            .ToList();
                                     eventJson.ForEach(@this => _eventSerializer.Deserialize(typeof(NewtonSoftEventStoreEventSerializerTests.TestEvent), @this));
                                 },
                                 maxTotal: allowedTime);
        }
    }
}
