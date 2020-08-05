using System;
using System.Reflection;
using Composable.SystemCE;
using Composable.SystemCE.DiagnosticsCE;
using Composable.SystemCE.ReflectionCE;
using Composable.Testing;
using Composable.Testing.Performance;
using FluentAssertions;
using JetBrains.Annotations;
using NUnit.Framework;

#pragma warning disable IDE1006 //Review OK: Test Naming Styles

namespace Composable.Tests.SystemCE.ReflectionCE
{
    [TestFixture]public class Activator_one_argument_constructor_performance_tests
    {
        static readonly string _argument = "AnArgument";

        [UsedImplicitly] class Simple
        {
#pragma warning disable IDE0060 //Review OK: unused parameter is intentional
            public Simple(string arg1){}
#pragma warning restore IDE0060 // Remove unused parameter
        }

        [Test] public void Can_create_instance() => Constructor.For<Simple>.WithArguments<string>.Instance(_argument).Should().NotBe(null);

        [Test] public void _005_Constructs_1_00_000_instances_within_60_percent_of_normal_constructor_call()
        {
            var constructions = 1_00_000.EnvDivide(instrumented:4.7);

            //warmup
            StopwatchCE.TimeExecution(DefaultConstructor, constructions);
            StopwatchCE.TimeExecution(DynamicModuleConstruct, constructions);


            var defaultConstructor = StopwatchCE.TimeExecution(DefaultConstructor, constructions).Total;
            var maxTime = defaultConstructor.MultiplyBy(1.60);
            TimeAsserter.Execute(DynamicModuleConstruct, constructions, maxTotal: maxTime);
        }

        [Test] public void _005_Constructs_1_00_000_instances_35_times_faster_than_via_activator_createinstance()
        {
            var constructions = 1_00_000.EnvDivide(instrumented:20);

            //warmup
            StopwatchCE.TimeExecution(ActivatorCreateInstance, constructions);
            StopwatchCE.TimeExecution(DynamicModuleConstruct, constructions);


            var defaultConstructor = StopwatchCE.TimeExecution(ActivatorCreateInstance, constructions).Total;
            var maxTime = defaultConstructor.DivideBy(35);
            TimeAsserter.Execute(DynamicModuleConstruct, constructions, maxTotal: maxTime.EnvMultiply(instrumented: 25));
        }

        static void DynamicModuleConstruct() => Constructor.For<Simple>.WithArguments<string>.Instance(_argument);

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
