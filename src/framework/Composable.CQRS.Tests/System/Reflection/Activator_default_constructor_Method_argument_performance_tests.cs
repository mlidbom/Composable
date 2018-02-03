using System;
using Composable.System.Diagnostics;
using Composable.System.Reflection;
using Composable.Testing.Performance;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Composable.Tests.System.Reflection
{
    [TestFixture]public class Activator_default_constructor_Method_argument_uncached_performance_tests
    {
        [UsedImplicitly] class Simple
        {
            public Simple(){}
        }


        [Test] public void CanCreateInstance()
        {
            var instance = Constructor.DefaultFor(typeof(Simple))();
        }

        [Test] public void _005_Constructs_10_000_000_2_times_fasterthan_via_activator_createinstance()
        {
            var constructions = 1_000_000.InstrumentationDecrease(10);

            //warmup
            StopwatchExtensions.TimeExecution(ActivatorCreateInstance, constructions);
            StopwatchExtensions.TimeExecution(DynamicModuleConstruct, constructions);


            var defaultConstructor = StopwatchExtensions.TimeExecution(ActivatorCreateInstance, constructions).Total;
            var maxTime = TimeSpan.FromMilliseconds(defaultConstructor.TotalMilliseconds * (1.0/2));
            TimeAsserter.Execute(DynamicModuleConstruct, constructions, maxTotal: maxTime.InstrumentationSlowdown(4.2));
        }

        static void DynamicModuleConstruct() => Constructor.DefaultFor(typeof(Simple))();

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
