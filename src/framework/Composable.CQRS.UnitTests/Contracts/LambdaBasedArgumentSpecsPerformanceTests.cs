using Composable.Contracts;
using Composable.Testing.Performance;
using FluentAssertions;
using NCrunch.Framework;
using NUnit.Framework;
using Composable.System;
using Composable.Testing;

namespace Composable.Tests.Contracts
{
    [TestFixture, Performance, Serial] public class LambdaBasedArgumentSpecsPerformanceTests
    {
        [Test] public void ShouldRun50TestsIn1Millisecond() //The expression compilation stuff was worrying but this should be OK except for tight loops.
        {
            var notNullOrDefault = new object();

            TimeAsserter.Execute(
                action: () => Contract.Argument(() => notNullOrDefault).NotNullOrDefault(),
                iterations: 500,
                maxTotal: 10.Milliseconds()
            );
        }
    }
}
