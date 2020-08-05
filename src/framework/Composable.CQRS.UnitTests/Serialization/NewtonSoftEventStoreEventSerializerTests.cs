using System;
using System.Linq;
using Composable.Logging;
using Composable.Persistence.EventStore;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.SystemCE;
using Composable.SystemCE.DiagnosticsCE;
using Composable.SystemCE.LinqCE;
using Composable.Testing;
using Composable.Testing.Performance;
using FluentAssertions;
using JetBrains.Annotations;
using NCrunch.Framework;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Composable.Tests.Serialization
{
    [TestFixture]
    public class NewtonSoftEventStoreEventSerializerTests
    {
        IEventStoreSerializer _eventSerializer;

        [OneTimeSetUp] public void SetupTask() => _eventSerializer = new EventStoreSerializer(new TypeMapper());

        internal class TestEvent : AggregateEvent
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

            ConsoleCE.WriteLine(eventWithAllValuesJson);

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
    }
}