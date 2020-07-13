﻿using System;
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
    [TestFixture, Performance]public class Activator_default_constructor_Generic_argument_performance_tests
    {
        [UsedImplicitly] class Simple
        {}

        [Test] public void Can_construct_instance() => Constructor.For<Simple>.DefaultConstructor.Instance().Should().NotBe(null);

        [Test] public void Constructs_1_000_000_instances_within_50_percent_of_default_constructor_time()
        {
            var constructions = 1_000_000.InstrumentationSlowdown(4.7);

            //warmup
            StopwatchExtensions.TimeExecution(DefaultConstructor, constructions);
            StopwatchExtensions.TimeExecution(DynamicModuleConstruct, constructions);


            var defaultConstructor = StopwatchExtensions.TimeExecution(DefaultConstructor, constructions).Total;
            var maxTime = defaultConstructor.MultiplyBy(1.50);
            TimeAsserter.Execute(DynamicModuleConstruct, constructions, maxTotal: maxTime);
        }

        [Test] public void Constructs_1_000_000_instances_3_times_faster_than_via_new_constraint_constructor_time()
        {
            var constructions = 1_000_000.InstrumentationSlowdown(10);

            //warmup
            StopwatchExtensions.TimeExecution(NewConstraint, constructions);
            StopwatchExtensions.TimeExecution(DynamicModuleConstruct, constructions);


            var defaultConstructor = StopwatchExtensions.TimeExecution(NewConstraint, constructions).Total;
            var maxTime = defaultConstructor.DivideBy(3);
            TimeAsserter.Execute(DynamicModuleConstruct, constructions, maxTotal: maxTime.InstrumentationSlowdown(4));
        }

        [Test] public void Constructs_1_000_000_instances_3_times_faster_than_via_activator_CreateInstance()
        {
            var constructions = 1_000_000.InstrumentationSlowdown(10);

            //warmup
            StopwatchExtensions.TimeExecution(ActivatorCreateInstance, constructions);
            StopwatchExtensions.TimeExecution(DynamicModuleConstruct, constructions);


            var defaultConstructor = StopwatchExtensions.TimeExecution(ActivatorCreateInstance, constructions).Total;
            var maxTime = defaultConstructor.DivideBy(3);
            TimeAsserter.Execute(DynamicModuleConstruct, constructions, maxTotal: maxTime.InstrumentationSlowdown(4.2));
        }

        static void DynamicModuleConstruct() => Constructor.For<Simple>.DefaultConstructor.Instance();

        // ReSharper disable once ObjectCreationAsStatement
        static void DefaultConstructor() => FakeActivator.CreateWithDefaultConstructor();

        static void ActivatorCreateInstance() => FakeActivator.CreateUsingActivatorCreateInstance();

        static void NewConstraint() => ActivateViaNewConstraint<Simple>.Create();

        static class FakeActivator
        {
            // ReSharper disable once ObjectCreationAsStatement
            internal static void CreateWithDefaultConstructor() => new Simple();
            internal static void CreateUsingActivatorCreateInstance() => Activator.CreateInstance(typeof(Simple), nonPublic: true);
        }

        static class ActivateViaNewConstraint<TInstance> where TInstance : new()
        {
            // ReSharper disable once ObjectCreationAsStatement
            internal static void Create() => new TInstance();
        }
    }
}
