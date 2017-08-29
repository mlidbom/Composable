using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Composable.System.Threading.ResourceAccess
{
    class AwaitingExclusiveResourcAccessLeaseTimeoutException : Exception
    {
        static TimeSpan _timeToWaitForOwningThreadStacktrace = 30.Seconds();

        internal static void TestingOnlyRunWithModifiedTimeToWaitForOwningThreadStacktrace(TimeSpan timeout, Action action)
        {
            var currentValue = _timeToWaitForOwningThreadStacktrace;
            using (Disposable.Create(() => _timeToWaitForOwningThreadStacktrace = currentValue))
            {
                _timeToWaitForOwningThreadStacktrace = timeout;
            }
        }


        readonly object _lockedObject;
        static readonly Dictionary<object, object> TimedOutLocks = new Dictionary<object, object>();

        internal static void ReportStackTraceIfError(object lockTarget)
        {
            lock (TimedOutLocks)
            {
                if (TimedOutLocks.ContainsKey(lockTarget))
                {
                    (TimedOutLocks[lockTarget] as ManualResetEvent)?.Set();
                    TimedOutLocks[lockTarget] = new StackTrace(fNeedFileInfo: true);
                }
            }
        }

        internal AwaitingExclusiveResourcAccessLeaseTimeoutException(object lockedObject) : base("Timed out awaiting exclusive access to resource.")
        {
            lock (TimedOutLocks)
            {
                TimedOutLocks[lockedObject] = new ManualResetEvent(false);
            }
            _lockedObject = lockedObject;
        }

        string _blockingThreadStacktrace;
        public override string Message
        {
            get
            {
                if (_blockingThreadStacktrace == null)
                {
                    ManualResetEvent waitHandle;
                    lock (TimedOutLocks)
                    {
                        waitHandle = TimedOutLocks[_lockedObject] as ManualResetEvent;
                    }
                    waitHandle?.WaitOne(_timeToWaitForOwningThreadStacktrace, exitContext: false);
                    lock (TimedOutLocks)
                    {
                        _blockingThreadStacktrace = (TimedOutLocks[_lockedObject] as StackTrace)?.ToString();
                    }
                }

                return $@"{base.Message}
----- Blocking thread stacktrace -----
{_blockingThreadStacktrace ?? "Failed to get blocking thread stack trace"}
";
            }
        }
    }
}
