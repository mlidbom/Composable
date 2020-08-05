using System;
using Composable.Contracts;
using Composable.SystemCE;
using Composable.Testing.Performance;
using NCrunch.Framework;
using NUnit.Framework;
using Composable.Testing;

namespace Composable.Tests.Contracts
{
    [TestFixture, Performance, Serial] public class ObjectNotDefaultPerformanceTests
    {
        [Test] public void ShouldRun300TestsIn1Milliseconds()
        {
            var one = 1;

            TimeAsserter.Execute(
                action: () => Contract.Argument(() => one).NotDefault(),
                iterations: 300,
                maxTotal: 1.Milliseconds().EnvMultiply(instrumented: 2));
        }

        [Test] public void ShouldRun300TestsIn1Millisecond()
        {
            var one = 1;

            TimeAsserter.Execute(
                action: () =>
                {
                    var inspected = Contract.Argument(() => one);
                    inspected.NotNullOrDefault();
                },
                iterations: 300,
                maxTotal: 1.Milliseconds().EnvMultiply(instrumented: 3));
        }
    }
}
