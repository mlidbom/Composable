using Composable.Contracts;
using Composable.Testing;
using Composable.Testing.Performance;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.Contracts {
    [TestFixture, Performance]
    public class LambdaBasedArgumentSpecsPerformanceTests
    {
        [Test]
        public void ShouldRun50TestsIn1Millisecond() //The expression compilation stuff was worrying but this should be OK except for tight loops.
        {
            var notNullOrDefault = new object();

            TimeAsserter.Execute(
                action: () => Contract.Argument(() => notNullOrDefault).NotNullOrDefault(),
                iterations: 500,
                maxTotal: 10.Milliseconds().AdjustRuntimeToTestEnvironment()
            );
        }
    }
}