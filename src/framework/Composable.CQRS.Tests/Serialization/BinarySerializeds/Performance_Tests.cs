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
            _instance = HasAllPropertyTypes.CreateInstanceWithSaneValues();

            _instance.RecursiveArrayProperty = new[]
                                              {
                                                  HasAllPropertyTypes.CreateInstanceWithSaneValues(),
                                                  null,
                                                  HasAllPropertyTypes.CreateInstanceWithSaneValues()
                                              };

            _instance.RecursiveListProperty = new List<HasAllPropertyTypes>()
                                             {
                                                 HasAllPropertyTypes.CreateInstanceWithSaneValues(),
                                                 null,
                                                 HasAllPropertyTypes.CreateInstanceWithSaneValues()
                                             };

            _serialized = _instance.Serialize();

            //Warmup
            RunScenario(DefaultConstructor, 1000);
            RunScenario(DynamicModuleConstruct, 1000);
            RunScenario(() => JsonRoundTrip(_instance), 1000);
            RunScenario(BinaryRoundTrip, iterations: 1000);

        }

        [Test] public void Instance_with_recursive_list_and_array_property_with_one_null_value_roundtrip_5_times_faster_than_NewtonSoft()
        {
            const int iterations = 1_000;


            var jsonSerializationTime = RunScenario(() => JsonRoundTrip(_instance, 1), iterations);

            var maxTotal = TimeSpan.FromMilliseconds(jsonSerializationTime.TotalMilliseconds / 5);

            var binarySerializationTime = RunScenario(BinaryRoundTrip, iterations.InstrumentationDecrease(5), maxTotal:maxTotal);

            Console.WriteLine($"Binary: {binarySerializationTime.TotalMilliseconds}, JSon: {jsonSerializationTime.TotalMilliseconds}");
        }

        [Test] public void _005_Constructs_1_00_000_instances_within_5_percent_of_default_constructor_time()
        {
            var constructions = 1_00_000.InstrumentationDecrease(4.7);
            var defaultConstructor = RunScenario(DefaultConstructor, constructions);
            var maxTime = TimeSpan.FromMilliseconds(defaultConstructor.TotalMilliseconds * 1.05);
            RunScenario(DynamicModuleConstruct, constructions, maxTotal: maxTime );
        }

        [Test] public void _010_Serializes_10_000_times_in_100_milliseconds() =>
            RunScenario(BinarySerialize, 10_000.InstrumentationDecrease(6.5), maxTotal:100.Milliseconds());

        [Test] public void _020_DeSerializes_10_000_times_in_130_milliseconds() =>
                RunScenario(BinaryDeSerialize, iterations: 10_000.InstrumentationDecrease(5.5), maxTotal:130.Milliseconds());

        [Test] public void _030_Roundtrips_10_000_times_in_220_milliseconds() =>
            RunScenario(BinaryRoundTrip, iterations: 10_000.InstrumentationDecrease(6.5), maxTotal:220.Milliseconds());

        //ncrunch: no coverage start

        static TimeSpan RunScenario(Action action, int iterations, TimeSpan? maxTotal = null)
        {
            if(maxTotal != null)
            {
                return TimeAsserter.Execute(action, iterations: iterations, maxTotal: maxTotal).Total;
            } else
            {
                return StopwatchExtensions.TimeExecution(action, iterations: iterations).Total;
            }
        }

        static void JsonRoundTrip(HasAllPropertyTypes instance, int iterations = 1)
        {
            for(int i = 0; i < iterations; i++)
            {
                var data = JsonConvert.SerializeObject(instance);
                instance = JsonConvert.DeserializeObject<HasAllPropertyTypes>(data);
            }
        }

        void BinaryRoundTrip() => BinarySerialized<HasAllPropertyTypes>.Deserialize(_instance.Serialize());

        void BinarySerialize() => _instance.Serialize();

        void BinaryDeSerialize() => BinarySerialized<HasAllPropertyTypes>.Deserialize(_serialized);

        static void DynamicModuleConstruct() => BinarySerialized<HasAllPropertyTypes>.DefaultConstructor();

        // ReSharper disable once ObjectCreationAsStatement
        static void DefaultConstructor() => new HasAllPropertyTypes();

        //ncrunch: no coverage end

    }

    static class Percenter
    {
        internal static string PercentOf(this TimeSpan @this, TimeSpan other) => ((int)(100 * (@this.TotalMilliseconds / other.TotalMilliseconds -1))).ToString("00");
    }
}
