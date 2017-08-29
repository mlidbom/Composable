using System;
using System.Diagnostics;
using System.Threading;

namespace Composable.System.Threading.ResourceAccess
{
    class AwaitingExclusiveResourceLockTimeoutException : Exception
    {
        static TimeSpan _timeToWaitForOwningThreadStacktrace = 30.Seconds();

        internal static void TestingOnlyRunWithModifiedTimeToWaitForOwningThreadStacktrace(TimeSpan timeout, Action action)
        {
            var currentValue = _timeToWaitForOwningThreadStacktrace;
            using(Disposable.Create(() => _timeToWaitForOwningThreadStacktrace = currentValue))
            {
                _timeToWaitForOwningThreadStacktrace = timeout;
            }
        }

        internal AwaitingExclusiveResourceLockTimeoutException() : base("Timed out awaiting exclusive access to resource.") { }

        string _blockingThreadStacktrace;
        readonly ManualResetEvent _blockingStacktraceWaitHandle = new ManualResetEvent(false);

        public override string Message
        {
            get
            {
                _blockingStacktraceWaitHandle.WaitOne(_timeToWaitForOwningThreadStacktrace);
                Interlocked.CompareExchange(ref _blockingThreadStacktrace, "Failed to get blocking thread stack trace", null);

                return $@"{base.Message}
----- Blocking threads stacktrace for disposing its lock -----
{_blockingThreadStacktrace}
";
            }
        }

        internal void SetBlockingThreadsDisposeStackTrace(StackTrace blockingThreadStackTrace)
        {
            Interlocked.CompareExchange(ref _blockingThreadStacktrace, blockingThreadStackTrace.ToString(), null);
            _blockingStacktraceWaitHandle.Set();
        }
    }
}
