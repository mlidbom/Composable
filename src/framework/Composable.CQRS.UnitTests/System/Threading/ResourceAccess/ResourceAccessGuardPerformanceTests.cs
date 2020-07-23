using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using Composable.Testing.Performance;
using Xunit;
using Composable.Testing;
using NCrunch.Framework;

namespace Composable.Tests.System.Threading.ResourceAccess
{
    [Performance, Serial]
    public class ResourceAccessGuardPerformanceTests
    {
        [Fact] public void Multiple_threads_take_10_000_update_locks_in_6_milliseconds()
        {
            var guard = ResourceGuard.WithTimeout(100.Milliseconds());

            const int totalLocks = 10_000;
            const int iterations = 100;
            const int locksPerIteration = totalLocks / iterations;

            //ncrunch: no coverage start
            void HammerUpdateLocks()
            {
                for(var i = 0; i < locksPerIteration; i++)
                    using(guard.AwaitUpdateLock()) {}
            }
            //ncrunch: no coverage end

            TimeAsserter.ExecuteThreaded(HammerUpdateLocks,
                                         iterations,
                                         description: $"Take {locksPerIteration} update locks",
                                         maxTotal: 6.Milliseconds().IfInstrumentedMultiplyBy(10));
        }

        [Fact] public void Multiple_threads_take_10_000_read_locks_in_6_milliseconds()
        {
            var guard = ResourceGuard.WithTimeout(100.Milliseconds());

            const int totalLocks = 10_000;
            const int iterations = 100;
            const int locksPerIteration = totalLocks / iterations;

            //ncrunch: no coverage start
            void HammerUpdateLocks()
            {
                for(var i = 0; i < locksPerIteration; i++)
                    using(guard.AwaitUpdateLock()) {}
            }
            //ncrunch: no coverage end

            TimeAsserter.ExecuteThreaded(HammerUpdateLocks,
                                         iterations,
                                         description: $"Take {locksPerIteration} update locks",
                                         maxTotal: 6.Milliseconds().IfInstrumentedMultiplyBy(10));
        }
    }
}
