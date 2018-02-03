using System;
using Composable.System.Diagnostics;
using Composable.System.Reflection;
using Composable.Testing.Performance;
using FluentAssertions;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Composable.Tests.System.Reflection
{
    [TestFixture]public class Activator_default_constructor_Method_argument_uncached_performance_tests
    {
        [UsedImplicitly] class Simple
        {}


        [Test] public void CanCreateInstance() => Constructor.DefaultFor(typeof(Simple))().Should().NotBe(null);

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

        static void ActivatorCreateInstance() => FakeActivator.CreateUsingActivatorCreateInstance();


        static class FakeActivator
        {
            internal static void CreateUsingActivatorCreateInstance() => Activator.CreateInstance(typeof(Simple), nonPublic: true);
        }
    }
}
