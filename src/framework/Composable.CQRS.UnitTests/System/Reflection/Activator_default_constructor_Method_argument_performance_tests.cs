using System;
using Composable.System;
using Composable.System.Diagnostics;
using Composable.System.Reflection;
using Composable.Testing;
using Composable.Testing.Performance;
using FluentAssertions;
using JetBrains.Annotations;
using NCrunch.Framework;
using NUnit.Framework;

namespace Composable.Tests.System.Reflection
{
    [TestFixture][Performance][Serial]public class Activator_default_constructor_Method_argument_uncached_performance_tests
    {
        [UsedImplicitly] class Simple
        {}


        [Test][Serial] public void CanCreateInstance() => Constructor.DefaultConstructorFor(typeof(Simple))().Should().NotBe(unexpected: null);

        [Test][Serial] public void _005_Constructs_100_000_instances_within_30_percent_of_the_performance_of_activator_createinstance()
        {
            var constructions = 100_000.IfInstrumentedDivideBy(@by: 10);

            //warmup
            StopwatchExtensions.TimeExecution(ActivatorCreateInstance, constructions);
            StopwatchExtensions.TimeExecution(() => DynamicModuleConstruct(), constructions);


            var defaultConstructor = StopwatchExtensions.TimeExecution(ActivatorCreateInstance, constructions).Total;
            TimeAsserter.Execute(() => DynamicModuleConstruct(), constructions, maxTotal: defaultConstructor.IfInstrumentedMultiplyBy(@by: 2) * 1.3, maxTries: 10);
        }

        static Func<object> DynamicModuleConstruct => Constructor.DefaultConstructorFor(typeof(Simple));

        // ReSharper disable once ObjectCreationAsStatement

        static void ActivatorCreateInstance() => FakeActivator.CreateUsingActivatorCreateInstance();


        static class FakeActivator
        {
            internal static void CreateUsingActivatorCreateInstance() => Activator.CreateInstance(typeof(Simple), nonPublic: true);
        }
    }
}
