using Composable.System.Threading.ResourceAccess;
using Composable.Testing.Performance;
using FluentAssertions;
using Xunit;

namespace Composable.Tests.System.Threading.ResourceAccess
{
    public class ResourceAccessGuardPerformanceTests
    {
        [Fact] void Multiple_threads_take_100_000_update_locks_in_40_milliseconds()
        {
            var guard = GuardedResource.WithTimeout(100.Milliseconds());

            const int totalLocks = 100_000;
            const int iterations = 100;
            const int locksPerIteration = totalLocks / iterations;

            void HammerUpdateLocks()
            {
                for (var i = 0; i < locksPerIteration; i++)
                {
                    using(guard.AwaitUpdateLock()) {}
                }
            }

            TimeAsserter.ExecuteThreaded(HammerUpdateLocks,
                                         iterations: iterations,
                                         description: $"Take {locksPerIteration} update locks",
                                         maxTotal: 40.Milliseconds().NCrunchSlowdownFactor(11));
        }

        [Fact] void Multiple_threads_take_100_000_read_locks_in_40_milliseconds()
        {
            var guard = GuardedResource.WithTimeout(100.Milliseconds());

            const int totalLocks = 100_000;
            const int iterations = 100;
            const int locksPerIteration = totalLocks / iterations;

            void HammerUpdateLocks()
            {
                for (var i = 0; i < locksPerIteration; i++)
                {
                    using(guard.AwaitUpdateLock()) {}
                }
            }

            TimeAsserter.ExecuteThreaded(HammerUpdateLocks,
                                         iterations: iterations,
                                         description: $"Take {locksPerIteration} update locks",
                                         maxTotal: 40.Milliseconds().NCrunchSlowdownFactor(11));
        }
    }
}
