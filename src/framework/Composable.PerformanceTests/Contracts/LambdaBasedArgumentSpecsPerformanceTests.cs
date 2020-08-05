using System;
using Composable.Contracts;
using Composable.SystemCE;
using Composable.Testing.Performance;
using NCrunch.Framework;
using NUnit.Framework;
using Composable.Testing;

namespace Composable.Tests.Contracts
{
    [TestFixture, Performance, Serial] public class LambdaBasedArgumentSpecsPerformanceTests
    {
        [Test] public void ShouldRun300TestsIn1Millisecond() //The expression compilation stuff was worrying but this should be OK except for tight loops.
        {
            var notNullOrDefault = new object();

            TimeAsserter.Execute(
                action: () => Contract.Argument(() => notNullOrDefault).NotNullOrDefault(),
                iterations: 300,
                maxTotal: 1.Milliseconds().EnvMultiply(instrumented: 3.0)
            );
        }
    }
}

