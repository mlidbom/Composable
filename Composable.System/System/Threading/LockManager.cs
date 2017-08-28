using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Composable.System.Threading
{
    interface IObjectLock
    {
        IDisposable LockForExclusiveUse();
        IDisposable LockForExclusiveUse(TimeSpan timeout);
    }

    static class ObjectLock
    {
        public static IObjectLock WithTimeout(TimeSpan timeout) => new ObjectLockInstance(timeout);

        public static void RunWithExclusiveLock(this IObjectLock @lock, Action action)
        {
            using (@lock.LockForExclusiveUse())
            {
                action();
            }
        }

        public static TResult RunWithExclusiveLock<TResult>(this IObjectLock @lock, Func<TResult> function)
        {
            using (@lock.LockForExclusiveUse())
            {
                return function();
            }
        }

        public static void RunWithExclusiveLock(this IObjectLock @lock, TimeSpan timeout, Action action)
        {
            using (@lock.LockForExclusiveUse(timeout))
            {
                action();
            }
        }

        public static TResult RunWithExclusiveLock<TResult>(this IObjectLock @lock, TimeSpan timeout, Func<TResult> function)
        {
            using (@lock.LockForExclusiveUse(timeout))
            {
                return function();
            }
        }

        class ObjectLockInstance : IObjectLock
        {
            readonly object _lockedObject;
            readonly TimeSpan _timeout;

            public ObjectLockInstance(TimeSpan timeout) : this(new object(), timeout) { }
            public ObjectLockInstance(object lockedObject, TimeSpan timeout)
            {
                _lockedObject = lockedObject;
                _timeout = timeout;
            }

            public IDisposable LockForExclusiveUse() => InternalLockForExclusiveUse(null);
            public IDisposable LockForExclusiveUse(TimeSpan timeout) => InternalLockForExclusiveUse(timeout);

            IDisposable InternalLockForExclusiveUse(TimeSpan? timeout = null)
            {
                if (!Monitor.TryEnter(_lockedObject, timeout ?? _timeout))
                {
                    throw new ObjectLockTimedOutException(_lockedObject);
                }

                return Disposable.Create(
                    () =>
                    {
                        try
                        {
                            ObjectLockTimedOutException.ReportStackTraceIfError(_lockedObject);
                        }
                        finally
                        {
                            Monitor.Exit(_lockedObject);
                        }
                    });
            }
        }
    }

    class ObjectLockTimedOutException : Exception
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


        readonly object _lockedObject;
        static readonly Dictionary<object, object> TimedOutLocks = new Dictionary<object, object>();

        internal static void ReportStackTraceIfError(object lockTarget)
        {
            lock(TimedOutLocks)
            {
                if(TimedOutLocks.ContainsKey(lockTarget))
                {
                    (TimedOutLocks[lockTarget] as ManualResetEvent)?.Set();
                    TimedOutLocks[lockTarget] = new StackTrace(fNeedFileInfo:true);
                }
            }
        }

        internal ObjectLockTimedOutException(object lockedObject) : base("Timeout waiting for lock")
        {
            lock(TimedOutLocks)
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
                if(_blockingThreadStacktrace == null)
                {
                    ManualResetEvent waitHandle;
                    lock(TimedOutLocks)
                    {
                        waitHandle = TimedOutLocks[_lockedObject] as ManualResetEvent;
                    }
                    waitHandle?.WaitOne(_timeToWaitForOwningThreadStacktrace, exitContext: false);
                    lock(TimedOutLocks)
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
