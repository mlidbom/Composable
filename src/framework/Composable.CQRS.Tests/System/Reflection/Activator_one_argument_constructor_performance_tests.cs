using System;
using System.Reflection;
using Composable.System.Diagnostics;
using Composable.System.Reflection;
using Composable.Testing.Performance;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Composable.Tests.System.Reflection
{
    [TestFixture]public class Activator_one_argument_constructor_performance_tests
    {
        static string _argument = "AnArgument";

        [UsedImplicitly] class Simple
        {
            public Simple(string arg1){}
        }

        [Test] public void Can_create_instance()
        {
            var instance = Activator<Simple>.OneArgumentConstructor<string>.Instance(_argument);
        }

        [Test] public void _005_Constructs_1_000_000_instances_within_15_percent_of_normal_constructor_call()
        {
            var constructions = 1_000_000.InstrumentationSlowdown(4.7);

            //warmup
            StopwatchExtensions.TimeExecution(DefaultConstructor, constructions);
            StopwatchExtensions.TimeExecution(DynamicModuleConstruct, constructions);


            var defaultConstructor = StopwatchExtensions.TimeExecution(DefaultConstructor, constructions).Total;
            var maxTime = TimeSpan.FromMilliseconds(defaultConstructor.TotalMilliseconds * 1.15);
            TimeAsserter.Execute(DynamicModuleConstruct, constructions, maxTotal: maxTime);
        }

        [Test] public void _005_Constructs_1_000_000_80_times_faster_than_via_activator_createinstance()
        {
            var constructions = 1_000_000.InstrumentationSlowdown(4.7);

            //warmup
            StopwatchExtensions.TimeExecution(ActivatorCreateInstance, constructions);
            StopwatchExtensions.TimeExecution(DynamicModuleConstruct, constructions);


            var defaultConstructor = StopwatchExtensions.TimeExecution(ActivatorCreateInstance, constructions).Total;
            var maxTime = TimeSpan.FromMilliseconds(defaultConstructor.TotalMilliseconds * (1.0/80));
            TimeAsserter.Execute(DynamicModuleConstruct, constructions, maxTotal: maxTime);
        }

        static void DynamicModuleConstruct() => Activator<Simple>.OneArgumentConstructor<string>.Instance(_argument);

        // ReSharper disable once ObjectCreationAsStatement
        static void DefaultConstructor() => FakeActivator.CreateWithDefaultConstructor();

        static void ActivatorCreateInstance() => FakeActivator.CreateUsingActivatorCreateInstance();


        static class FakeActivator
        {
            internal static Simple CreateWithDefaultConstructor() => new Simple(_argument);

            internal static Simple CreateUsingActivatorCreateInstance() => (Simple)Activator.CreateInstance(
                type: typeof(Simple),
                bindingAttr: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: new []{_argument},
                culture: null);
        }
    }
}
