using System;
using System.Threading;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using Composable.Testing.Performance;
using Composable.Testing;
using NCrunch.Framework;
using NUnit.Framework;

namespace Composable.Tests.System.Threading.ResourceAccess
{
    [Performance, Serial] public class ResourceAccessGuardPerformanceTests
    {
        [SetUp] public void SetupTask()
        {
            _doSomething = false;
        }

        [Test] public void Average_uncontended_WithExclusiveAccess_time_is_less_than_200_nanoseconds()
        {
            var guard = ResourceGuard.WithTimeout(100.Milliseconds());

            const int totalLocks = 100_000;

            //ncrunch: no coverage start
            void HammerUpdateLocks()
            {
                for(var i = 0; i < totalLocks; i++)
                    guard.WithExclusiveAccess(DoNothing);
            }
            //ncrunch: no coverage end

            TimeAsserter.Execute(HammerUpdateLocks, maxTotal: 200.Nanoseconds().IfInstrumentedMultiplyBy(8) * totalLocks);
        }

        [Test] public void Average_contended_WithExclusiveAccess_time_is_less_than_1_microsecond()
        {
            var guard = ResourceGuard.WithTimeout(100.Milliseconds());

            const int totalLocks = 100_000;
            const int iterations = 100;
            const int locksPerIteration = totalLocks / iterations;

            //ncrunch: no coverage start
            void HammerUpdateLocks()
            {
                for(var i = 0; i < locksPerIteration; i++)
                    guard.WithExclusiveAccess(DoNothing);
            }
            //ncrunch: no coverage end

            TimeAsserter.ExecuteThreadedLowOverhead(HammerUpdateLocks,
                                         iterations,
                                         description: $"Take {locksPerIteration} update locks",
                                         maxTotal: totalLocks.Microseconds().IfInstrumentedMultiplyBy(1.5));
        }

        [Test] public void Average_contended_ReadLock_time_is_less_than_1_microsecond()
        {
            var guard = ResourceGuard.WithTimeout(1.Seconds());

            const int totalLocks = 100_000;
            const int iterations = 100;
            const int locksPerIteration = totalLocks / iterations;

            //ncrunch: no coverage start
            void HammerUpdateLocks()
            {
                for(var i = 0; i < locksPerIteration; i++)
                    using(guard.AwaitReadLock())
                    {
                        DoNothing();
                    }
            }
            //ncrunch: no coverage end

            TimeAsserter.ExecuteThreadedLowOverhead(HammerUpdateLocks,
                                                  iterations,
                                                  description: $"Take {locksPerIteration} update locks",
                                                  maxTotal: totalLocks.Microseconds().IfInstrumentedMultiplyBy(3));
        }

        [Test] public void Average_uncontended_ReadLock_time_is_less_than_200_nanoseconds()
        {
            var guard = ResourceGuard.WithTimeout(100.Milliseconds());

            const int totalLocks = 100_000;

            //ncrunch: no coverage start
            void HammerUpdateLocks()
            {
                for(var i = 0; i < totalLocks; i++)
                    using(guard.AwaitReadLock())
                    {
                        DoNothing();
                    }
            }
            //ncrunch: no coverage end

            TimeAsserter.Execute(HammerUpdateLocks, maxTotal: 200.Nanoseconds().IfInstrumentedMultiplyBy(8) * totalLocks);
        }

        [Test] public void Average_contended_ExclusiveLock_time_is_less_than_1_microsecond()
        {
            var guard = ResourceGuard.WithTimeout(1.Seconds());

            const int totalLocks = 100_000;
            const int iterations = 100;
            const int locksPerIteration = totalLocks / iterations;

            //ncrunch: no coverage start
            void HammerUpdateLocks()
            {
                for(var i = 0; i < locksPerIteration; i++)
                    using(guard.AwaitExclusiveLock())
                    {
                        DoNothing();
                    }
            }
            //ncrunch: no coverage end

            TimeAsserter.ExecuteThreadedLowOverhead(HammerUpdateLocks,
                                                  iterations,
                                                  description: $"Take {locksPerIteration} update locks",
                                                  maxTotal: totalLocks.Microseconds().IfInstrumentedMultiplyBy(3));
        }

        [Test] public void Average_uncontended_ExclusiveLock_time_is_less_than_250_nanoseconds()
        {
            var guard = ResourceGuard.WithTimeout(100.Milliseconds());

            const int totalLocks = 100_000;

            //ncrunch: no coverage start
            void HammerUpdateLocks()
            {
                for(var i = 0; i < totalLocks; i++)
                    using(guard.AwaitExclusiveLock())
                    {
                        DoNothing();
                    }
            }
            //ncrunch: no coverage end

            TimeAsserter.Execute(HammerUpdateLocks, maxTotal: 250.Nanoseconds().IfInstrumentedMultiplyBy(8) * totalLocks);
        }

        bool _doSomething = true;
        void DoNothing()
        {
            if(_doSomething)
            {
                Console.WriteLine("Something");
            }
        }
    }
}
