using System.Threading;
using Composable.System;
using Composable.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.Testing
{
    using Composable.System;

    [TestFixture] public class TimeAsserterTests
    {
        [Test] public void Execute_should_add_at_most_1_milliseconds_to_1000_iterations_of_action()
        {
            TimeAsserter.Execute(
                setup: () => {},
                tearDown: () => {},
                action: () => {},
                iterations: 1000,
                timeFormat:"ffff",
                maxTotal: TimeSpanConversionExtensions.Milliseconds(1),
                maxTries: 5
            );
        }

        [Test] public void ExecuteThreaded_should_add_at_most_1_milliseconds_to_1000_iterations_of_action()
        {
            TimeAsserter.ExecuteThreaded(
                setup: () => {},
                tearDown: () => {},
                action: () => {},
                iterations: 1000,
                timeFormat: "ffff",
                maxTotal: TimeSpanConversionExtensions.Milliseconds(1),
                maxTries: 5
            );
        }
    }
}
