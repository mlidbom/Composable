using Composable.Contracts;
using Composable.Testing.Performance;
using FluentAssertions;
using NCrunch.Framework;
using NUnit.Framework;
using Composable.System;
using Composable.Testing;

namespace Composable.Tests.Contracts
{
    [TestFixture, Performance, Serial] public class ObjectNotDefaultPerformanceTests
    {
        [Test] public void ShouldRun500TestsIn10Milliseconds() //The Activator.CreateInstance stuff in the default check had me a bit worried. Seems I had no reason to be.
        {
            var one = 1;

            TimeAsserter.Execute(
                action: () => Contract.Argument(one, nameof(one)).NotDefault(),
                iterations: 500,
                maxTotal: 10.Milliseconds());
        }

        [Test] public void ShouldRun50TestsInOneMillisecond() //The Activator.CreateInstance stuff in the default check had me a bit worried. Seems I had no reason to be.
        {
            var one = 1;

            TimeAsserter.Execute(
                action: () =>
                {
                    var inspected = Contract.Argument(one, nameof(one));
                    inspected.NotNullOrDefault();
                },
                iterations: 500,
                maxTotal: 10.Milliseconds());
        }
    }
}
