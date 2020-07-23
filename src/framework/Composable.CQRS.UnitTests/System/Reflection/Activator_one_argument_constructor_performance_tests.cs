using System;
using System.Reflection;
using Composable.SystemCE;
using Composable.SystemCE.Diagnostics;
using Composable.SystemCE.Reflection;
using Composable.Testing;
using Composable.Testing.Performance;
using FluentAssertions;
using JetBrains.Annotations;
using NCrunch.Framework;
using NUnit.Framework;
#pragma warning disable IDE1006 //Review OK: Test Naming Styles

namespace Composable.Tests.System.Reflection
{
    [TestFixture, Performance, Serial]public class Activator_one_argument_constructor_performance_tests
    {
        static readonly string _argument = "AnArgument";

        [UsedImplicitly] class Simple
        {
#pragma warning disable IDE0060 //Review OK: unused parameter is intentional
            public Simple(string arg1){}
#pragma warning restore IDE0060 // Remove unused parameter
        }

        [Test, Serial] public void Can_create_instance() => Constructor.For<Simple>.WithArgument<string>.Instance(_argument).Should().NotBe(null);

        [Test, Serial] public void _005_Constructs_1_00_000_instances_within_60_percent_of_normal_constructor_call()
        {
            var constructions = 1_00_000.IfInstrumentedDivideBy(4.7);

            //warmup
            StopwatchExtensions.TimeExecution(DefaultConstructor, constructions);
            StopwatchExtensions.TimeExecution(DynamicModuleConstruct, constructions);


            var defaultConstructor = StopwatchExtensions.TimeExecution(DefaultConstructor, constructions).Total;
            var maxTime = defaultConstructor.MultiplyBy(1.60);
            TimeAsserter.Execute(DynamicModuleConstruct, constructions, maxTotal: maxTime);
        }

        [Test, Serial] public void _005_Constructs_1_00_000_instances_35_times_faster_than_via_activator_createinstance()
        {
            var constructions = 1_00_000.IfInstrumentedDivideBy(20);

            //warmup
            StopwatchExtensions.TimeExecution(ActivatorCreateInstance, constructions);
            StopwatchExtensions.TimeExecution(DynamicModuleConstruct, constructions);


            var defaultConstructor = StopwatchExtensions.TimeExecution(ActivatorCreateInstance, constructions).Total;
            var maxTime = defaultConstructor.DivideBy(35);
            TimeAsserter.Execute(DynamicModuleConstruct, constructions, maxTotal: maxTime.IfInstrumentedMultiplyBy(25), timeFormat: "ss\\.ffff");
        }

        static void DynamicModuleConstruct() => Constructor.For<Simple>.WithArgument<string>.Instance(_argument);

        // ReSharper disable once ObjectCreationAsStatement
        static void DefaultConstructor() => FakeActivator.CreateWithDefaultConstructor();

        static void ActivatorCreateInstance() => FakeActivator.CreateUsingActivatorCreateInstance();


        static class FakeActivator
        {
            // ReSharper disable once ObjectCreationAsStatement
            internal static void CreateWithDefaultConstructor() => new Simple(_argument);

            internal static void CreateUsingActivatorCreateInstance() => Activator.CreateInstance(
                type: typeof(Simple),
                bindingAttr: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: new object[]{_argument},
                culture: null);
        }
    }
}
