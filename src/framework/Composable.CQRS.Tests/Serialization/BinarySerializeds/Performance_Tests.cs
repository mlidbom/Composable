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

            var binarySerializationTime = RunScenario(BinaryRoundTrip, iterations.InstrumentationSlowdown(5), maxTotal:maxTotal);

            Console.WriteLine($"Binary: {binarySerializationTime.TotalMilliseconds}, JSon: {jsonSerializationTime.TotalMilliseconds}");
        }

        [Test] public void _001_Compare_construction_strategies_1_00_000_instances_within_40_percent_of_default_constructor_time()
        {
            int constructions = 100_000;
            for(int i = 0; i < 10; i++)
            {
                var dynamicModule = RunScenario(DynamicModuleConstruct, iterations: constructions);
                var activator = RunScenario(ActivatorConstruct, iterations: constructions);
                var newConstraintConstructor = RunScenario(ConstructorConstruct, iterations: constructions);
                var constructor = RunScenario(DefaultConstructor, iterations: constructions);

                Console.WriteLine($"{nameof(constructor)}: {constructor.PercentOf(constructor)}, {nameof(dynamicModule)}: {dynamicModule.PercentOf(constructor)}, {nameof(newConstraintConstructor)}: {newConstraintConstructor.PercentOf(constructor)}, {nameof(activator)}: {activator.PercentOf(constructor)}");
            }
        }


        [Test] public void _005_Constructs_1_00_000_instances_within_40_percent_of_default_constructor_time()
        {
            var constructions = 1_00_000;
            var defaultConstructor = RunScenario(DefaultConstructor, constructions.InstrumentationSlowdown(4.7));
            var maxTime = TimeSpan.FromMilliseconds(defaultConstructor.TotalMilliseconds * 1.4);
            RunScenario(DynamicModuleConstruct, constructions.InstrumentationSlowdown(4.7), maxTotal: maxTime );
        }

        [Test] public void _010_Serializes_10_000_times_in_100_milliseconds() =>
            RunScenario(BinarySerialize, 10_000.InstrumentationSlowdown(6.5), maxTotal:100.Milliseconds());

        [Test] public void _020_DeSerializes_10_000_times_in_130_milliseconds() =>
                RunScenario(BinaryDeSerialize, iterations: 10_000.InstrumentationSlowdown(5.5), maxTotal:130.Milliseconds());

        [Test] public void _030_Roundtrips_10_000_times_in_220_milliseconds() =>
            RunScenario(BinaryRoundTrip, iterations: 10_000.InstrumentationSlowdown(6.5), maxTotal:220.Milliseconds());

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

        static void DynamicModuleConstruct() => BinarySerialized<HasAllPropertyTypes>.DynamicModuleConstructor();

        static void ActivatorConstruct() => BinarySerialized<HasAllPropertyTypes>.ActivatorConstruct();

        static void ConstructorConstruct() => BinarySerialized<HasAllPropertyTypes>.ConstructorConstruct();

        static void DefaultConstructor() => new HasAllPropertyTypes();

        //ncrunch: no coverage end

    }

    static class Percenter
    {
        internal static string PercentOf(this TimeSpan @this, TimeSpan other) => ((int)(100 * (@this.TotalMilliseconds / other.TotalMilliseconds -1))).ToString("00");
    }
}
