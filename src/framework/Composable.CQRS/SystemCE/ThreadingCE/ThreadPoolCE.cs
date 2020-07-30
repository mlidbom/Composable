using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Composable.SystemCE.LinqCE;

namespace Composable.SystemCE.ThreadingCE
{
    static class ThreadPoolCE
    {
        internal static void TryToEnsureSufficientIdleThreadsToRunTasksConcurrently(int threadCount)
        {
            for(int tries = 1; Idle < threadCount && tries < 5; tries++)
            {
                var waitForAllThreadsToStart = new CountdownEvent(threadCount);
                Task.WaitAll(1.Through(threadCount).Select(index => Task.Run(() =>
                {
                    waitForAllThreadsToStart.Signal(1);
                    waitForAllThreadsToStart.Wait();
                })).ToArray());
            }
        }

        static int Executing => Max - Available;
        static int Live => ThreadPool.ThreadCount;
        static int Idle => Live - Executing;
        static long Queued => ThreadPool.PendingWorkItemCount;

        static int Max
        {
            get
            {
                ThreadPool.GetMaxThreads(out var maxThreads, out var _);
                return maxThreads;
            }
        }

        static int Available
        {
            get
            {
                ThreadPool.GetAvailableThreads(out var availableThreads, out var _);
                return availableThreads;
            }
        }
    }
}
