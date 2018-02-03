using System;
using System.Collections.Generic;
using Composable.Serialization;
using Composable.System.Diagnostics;
using Composable.Testing.Performance;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;

// ReSharper disable BuiltInTypeReferenceStyle
// ReSharper disable MemberCanBePrivate.Local we want the inspection of the objects to include all properties...
// ReSharper disable MemberCanBePrivate.Global

namespace Composable.Tests.Serialization.BinarySerializeds
{
    [TestFixture, NCrunch.Framework.Serial] public class Performance_tests
    {
        HasAllPropertyTypes _instance;
        byte[] _serialized;
        [SetUp] public void SetupTask()
        {
            _instance = HasAllPropertyTypes.CreateInstance();

            _instance.RecursiveArrayProperty = new[]
                                              {
                                                  HasAllPropertyTypes.CreateInstance(),
                                                  null,
                                                  HasAllPropertyTypes.CreateInstance()
                                              };

            _instance.RecursiveListProperty = new List<HasAllPropertyTypes>()
                                             {
                                                 HasAllPropertyTypes.CreateInstance(),
                                                 null,
                                                 HasAllPropertyTypes.CreateInstance()
                                             };

            _serialized = _instance.Serialize();

            //Warmup
            JsonRoundTrip(_instance, 1000);
            BinaryRoundTrip(_instance, 1000);

        }

        [Test] public void Instance_with_recursive_list_and_array_property_with_one_null_value_roundtrip_5_times_faster_than_NewtonSoft()
        {
            const int iterations = 1_000;


            var jsonSerializationTime = StopwatchExtensions.TimeExecution(() => JsonRoundTrip(_instance, iterations));

            var maxTotal = TimeSpan.FromMilliseconds(jsonSerializationTime.TotalMilliseconds / 5);

            var binarySerializationTime = TimeAsserter.Execute(() => BinaryRoundTrip(_instance, iterations), maxTotal:maxTotal.InstrumentationSlowdown(5));

            Console.WriteLine($"Binary: {binarySerializationTime.Total.TotalMilliseconds}, JSon: {jsonSerializationTime.TotalMilliseconds}");
        }
        [Test] public void _01_Serializes_1_000_times_in_10_milliseconds() =>
            TimeAsserter.Execute(() => BinarySerialize(_instance, 1_000), maxTotal:10.Milliseconds().InstrumentationSlowdown(4.7));

        [Test] public void _02_DeSerializes_1_000_times_in_13_milliseconds() =>
            TimeAsserter.Execute(() => BinaryDeSerialize(1_000), maxTotal:13.Milliseconds().InstrumentationSlowdown(4.3));

        [Test] public void _03_Roundtrips_1_000_times_in_22_milliseconds() =>
            TimeAsserter.Execute(() => BinaryRoundTrip(_instance, 1_000), maxTotal:22.Milliseconds().InstrumentationSlowdown(4.7));

        //ncrunch: no coverage start
        static void JsonRoundTrip(HasAllPropertyTypes instance, int iterations)
        {
            for(int i = 0; i < iterations; i++)
            {
                var data = JsonConvert.SerializeObject(instance);
                instance = JsonConvert.DeserializeObject<HasAllPropertyTypes>(data);
            }
        }

        static void BinaryRoundTrip(HasAllPropertyTypes instance, int iterations)
        {
            for(int i = 0; i < iterations; i++)
            {
                var data = instance.Serialize();
                instance = BinarySerialized<HasAllPropertyTypes>.Deserialize(data);
            }
        }

        static void BinarySerialize(HasAllPropertyTypes instance, int iterations)
        {
            for(int i = 0; i < iterations; i++)
            {
                instance.Serialize();
            }
        }

         void BinaryDeSerialize(int iterations)
        {
            for(int i = 0; i < iterations; i++)
            {
                BinarySerialized<HasAllPropertyTypes>.Deserialize(_serialized);
            }
        }
        //ncrunch: no coverage end
    }
}
