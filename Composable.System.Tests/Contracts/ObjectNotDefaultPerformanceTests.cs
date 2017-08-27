using Composable.Contracts;
using Composable.Testing;
using Composable.Testing.Performance;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.Contracts {
    [TestFixture, Performance]
    public class ObjectNotDefaultPerformanceTests
    {
        [Test]
        public void ShouldRun500TestsIn10Milliseconds() //The Activator.CreateInstance stuff in the default check had me a bit worried. Seems I had no reason to be.
        {
            var one = 1;

            TimeAsserter.Execute(
                action: () => Contract.Argument(() => one).NotDefault(),
                iterations: 500,
                maxTotal: 10.Milliseconds().AdjustRuntimeToTestEnvironment());
        }

        [Test]
        public void ShouldRun50TestsInOneMillisecond() //The Activator.CreateInstance stuff in the default check had me a bit worried. Seems I had no reason to be.
        {
            var one = 1;

            TimeAsserter.Execute(
                action: () => Contract.Argument(() => one).NotNullOrDefault(),
                iterations: 500,
                maxTotal: 10.Milliseconds().AdjustRuntimeToTestEnvironment());
        }
    }
}