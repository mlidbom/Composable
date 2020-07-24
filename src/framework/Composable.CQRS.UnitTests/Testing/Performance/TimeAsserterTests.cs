using Composable.SystemCE;
using Composable.Testing.Performance;
using NUnit.Framework;

namespace Composable.Tests.Testing.Performance
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
            //Warmup
            TimeAsserter.ExecuteThreaded(action: () => {}, iterations: 100, maxTotal: 100.Milliseconds());

            TimeAsserter.ExecuteThreaded(
                action: () => {},
                iterations: 100,
                maxTotal: 1.Milliseconds()
            );
        }
    }
}
