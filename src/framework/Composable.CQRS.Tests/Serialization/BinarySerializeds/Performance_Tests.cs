using System;
using System.Collections.Generic;
using Composable.Serialization;
using Composable.System.Diagnostics;
using Composable.Testing.Performance;
using Newtonsoft.Json;
using NUnit.Framework;

// ReSharper disable BuiltInTypeReferenceStyle
// ReSharper disable MemberCanBePrivate.Local we want the inspection of the objects to include all properties...
// ReSharper disable MemberCanBePrivate.Global

namespace Composable.Tests.Serialization.BinarySerializeds
{
    [TestFixture] public class Performance_tests
    {
        [Test] public void Instance_with_recursive_list_and_array_property_with_one_null_value_roundtrip_4_times_faster_than_NewtonSoft()
        {
            var instance = HasAllPropertyTypes.CreateInstance();

            instance.RecursiveArrayProperty = new[]
                                                                  {
                                                                      HasAllPropertyTypes.CreateInstance(),
                                                                      null,
                                                                      HasAllPropertyTypes.CreateInstance()
                                                                  };

            instance.RecursiveListProperty = new List<HasAllPropertyTypes>()
                                                                  {
                                                                      HasAllPropertyTypes.CreateInstance(),
                                                                      null,
                                                                      HasAllPropertyTypes.CreateInstance()
                                                                  };


            const int iterations = 10000;
           //Warmup
            JsonRoundTrip(instance, 100);
            BinaryRoundTrip(instance, 100);


            var jsonSerializationTime = StopwatchExtensions.TimeExecution(() => JsonRoundTrip(instance, iterations));

            var maxTotal = TimeSpan.FromMilliseconds(jsonSerializationTime.TotalMilliseconds / 5);

            var binarySerializationTime = TimeAsserter.Execute(() => BinaryRoundTrip(instance, iterations), maxTotal:maxTotal);

            Console.WriteLine($"Binary: {binarySerializationTime.Total.TotalMilliseconds}, JSon: {jsonSerializationTime.TotalMilliseconds}");
        }

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
        //ncrunch: no coverage end
    }
}
