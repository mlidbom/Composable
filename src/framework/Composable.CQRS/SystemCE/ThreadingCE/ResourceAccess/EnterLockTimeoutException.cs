using System;
using System.Diagnostics;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    public class EnterLockTimeoutException : Exception
    {
        // ReSharper disable once FieldCanBeMadeReadOnly.Local (Actually our tests that temporarily changes this through reflection stops working if it is readonly....)
        static TimeSpan _timeToWaitForOwningThreadStacktrace = 30.Seconds();

        readonly MonitorCE _monitor = MonitorCE.WithDefaultTimeout();

        internal EnterLockTimeoutException(ulong lockId) : base("Timed out awaiting lock. This likely indicates an in-memory deadlock. See below for the stacktrace of the blocking thread as it disposes the lock.") =>
            LockId = lockId;

        internal ulong LockId { get; }

        string? _blockingThreadStacktrace;

        public override string Message
        {
            get
            {
                if(!_monitor.TryAwait(_timeToWaitForOwningThreadStacktrace, () => _blockingThreadStacktrace != null))
                {
                    _blockingThreadStacktrace = $"Failed to get blocking thread stack trace. Timed out after: {_timeToWaitForOwningThreadStacktrace}";
                }

                return $@"{base.Message}
----- Blocking thread lock disposal stack trace-----
{_blockingThreadStacktrace}
";
            }
        }

        internal void SetBlockingThreadsDisposeStackTrace(StackTrace blockingThreadStackTrace) =>
            _monitor.Update(() => _blockingThreadStacktrace = blockingThreadStackTrace.ToString());
    }
}
