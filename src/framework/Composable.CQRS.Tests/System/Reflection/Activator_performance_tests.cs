using System;
using Composable.System.Diagnostics;
using Composable.System.Reflection;
using Composable.Testing.Performance;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Composable.Tests.System.Reflection
{
    [TestFixture]public class Activator_default_constructor_performance_tests
    {
        [UsedImplicitly] class Simple
        {
            public Simple(){}
        }

        [Test] public void TestName()
        {
            var instance = Activator<Simple>.DefaultConstructor.Instance();
        }

        [Test] public void _005_Constructs_10_000_000_instances_within_15_percent_of_default_constructor_time()
        {
            var constructions = 1_000_000.InstrumentationSlowdown(4.7);

            //warmup
            StopwatchExtensions.TimeExecution(DefaultConstructor, constructions);
            StopwatchExtensions.TimeExecution(DynamicModuleConstruct, constructions);


            var defaultConstructor = StopwatchExtensions.TimeExecution(DefaultConstructor, constructions).Total;
            var maxTime = TimeSpan.FromMilliseconds(defaultConstructor.TotalMilliseconds * 1.15);
            TimeAsserter.Execute(DynamicModuleConstruct, constructions, maxTotal: maxTime);
        }

        [Test] public void _005_Constructs_10_000_000_80_percent_faster_than_via_new_constraint_constructor_time()
        {
            var constructions = 1_000_000.InstrumentationSlowdown(4.7);

            //warmup
            StopwatchExtensions.TimeExecution(NewConstraint, constructions);
            StopwatchExtensions.TimeExecution(DynamicModuleConstruct, constructions);


            var defaultConstructor = StopwatchExtensions.TimeExecution(NewConstraint, constructions).Total;
            var maxTime = TimeSpan.FromMilliseconds(defaultConstructor.TotalMilliseconds * (0.2));
            TimeAsserter.Execute(DynamicModuleConstruct, constructions, maxTotal: maxTime);
        }

        [Test] public void _005_Constructs_10_000_000_80_percent_faster_than_via_activator_createinstance()
        {
            var constructions = 1_000_000.InstrumentationSlowdown(4.7);

            //warmup
            StopwatchExtensions.TimeExecution(ActivatorCreateInstance, constructions);
            StopwatchExtensions.TimeExecution(DynamicModuleConstruct, constructions);


            var defaultConstructor = StopwatchExtensions.TimeExecution(ActivatorCreateInstance, constructions).Total;
            var maxTime = TimeSpan.FromMilliseconds(defaultConstructor.TotalMilliseconds * (0.2));
            TimeAsserter.Execute(DynamicModuleConstruct, constructions, maxTotal: maxTime);
        }

        static void DynamicModuleConstruct() => Activator<Simple>.DefaultConstructor.Instance();

        // ReSharper disable once ObjectCreationAsStatement
        static void DefaultConstructor() => FakeActivator.CreateWithDefaultConstructor();

        static void ActivatorCreateInstance() => FakeActivator.CreateUsingActivatorCreateInstance();

        static void NewConstraint() => ActivateViaNewConstraint<Simple>.Create();

        static class FakeActivator
        {
            internal static Simple CreateWithDefaultConstructor() => new Simple();
            internal static Simple CreateUsingActivatorCreateInstance() => (Simple)Activator.CreateInstance(typeof(Simple), nonPublic: true);
        }

        static class ActivateViaNewConstraint<TInstance> where TInstance : new()
        {
            internal static TInstance Create() => new TInstance();
        }
    }
}
