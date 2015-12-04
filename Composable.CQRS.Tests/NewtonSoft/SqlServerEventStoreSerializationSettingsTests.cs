using System;
using Composable.CQRS.EventSourcing;
using Composable.NewtonSoft;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;

namespace CQRS.Tests.NewtonSoft
{
    [TestFixture]
    public class SqlServerEventStoreSerializationSettingsTests
    {
        private class TestEvent : AggregateRootEvent
        {
            public TestEvent(string test1, string test2)
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
            var eventWithAllValuesSet = new TestEvent("Test1", "Test2")
                                        {
                                            AggregateRootId =  Guid.NewGuid(),
                                            AggregateRootVersion =  2,
                                            EventId = Guid.NewGuid(),
                                            InsertAfter =  10,
                                            InsertBefore = 20,
                                            Replaces = 30,
                                            InsertionOrder = 40,
                                            UtcTimeStamp = DateTime.Now + 1.Minutes()};

            var eventWithOnlySubclassValues = new TestEvent("Test1", "Test2")
                                              {
                                                  EventId = Guid.Empty,
                                                  UtcTimeStamp = DateTime.MinValue
                                              };

            var eventWithAllValuesJson = JsonConvert.SerializeObject(eventWithAllValuesSet, JsonSettings.SqlEventStoreSerializerSettings);
            var eventWithOnlySubclassValuesJson = JsonConvert.SerializeObject(eventWithOnlySubclassValues, JsonSettings.SqlEventStoreSerializerSettings);
            var roundTripped = JsonConvert.DeserializeObject<TestEvent>(eventWithAllValuesJson, JsonSettings.SqlEventStoreSerializerSettings);

            Console.WriteLine(eventWithAllValuesJson);

            eventWithAllValuesJson.Should().Be("{\"Test1\":\"Test1\",\"Test2\":\"Test2\"}");
            eventWithAllValuesJson.Should().Be(eventWithOnlySubclassValuesJson);

            roundTripped.ShouldBeEquivalentTo(eventWithOnlySubclassValues,
                config => config.Excluding(@event => @event.UtcTimeStamp)//Timestamp is defaulted in the constructor used by serialization.
                        .Excluding(@event => @event.EventId)
                );
        }
    }
}