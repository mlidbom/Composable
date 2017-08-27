using Composable.Testing;
using Composable.Testing.Performance;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.Testing
{
    [TestFixture] public class TimeAsserterTests
    {
        [Test] public void Execute_should_add_at_most_1_milliseconds_to_100_iterations_of_action()
        {
            TimeAsserter.Execute(
                setup: () => {},
                tearDown: () => {},
                action: () => {},
                iterations: 100,
                timeFormat:"ffff",
                maxTotal: 1.Milliseconds()
            );
        }

        [Test] public void ExecuteThreaded_should_add_at_most_1_milliseconds_to_100_iterations_of_action()
        {
            TimeAsserter.ExecuteThreaded(
                setup: () => {},
                tearDown: () => {},
                action: () => {},
                iterations: 100,
                timeFormat: "ffff",
                maxTotal: 1.Milliseconds()
            );
        }
    }
}
