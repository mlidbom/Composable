using System;
using System.Diagnostics;
using System.Threading;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
#pragma warning disable CA1001 // Types that own disposable fields should be disposable,
                               // Yes, but this is an exception. No one is going to dispose it so let's not be cute and pretend that we handle it "correctly".
                               //This should be thrown very rarely. Only when you have severe application misbehavior. We accept suboptimal cleanup here.
    public class EnterLockTimeoutException : Exception
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
    {
        internal static void TestingOnlyRunWithAlternativeTimeToWaitForOwningThreadStacktrace(TimeSpan timeoutOverride, Action action)
        {
            var originalTimeout = _timeToWaitForOwningThreadStacktrace;
            using(DisposableCE.Create(() => _timeToWaitForOwningThreadStacktrace = originalTimeout))
            {
                _timeToWaitForOwningThreadStacktrace = timeoutOverride;
                action();
            }
        }

        static TimeSpan _timeToWaitForOwningThreadStacktrace = 30.Seconds();

        internal EnterLockTimeoutException() : base("Timed out awaiting exclusive access to resource.") {}

        string? _blockingThreadStacktrace;
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
