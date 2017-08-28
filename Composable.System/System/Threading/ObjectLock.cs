using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Composable.System.Threading
{
    interface IObjectLock
    {
        [Obsolete("This is a low level abstraction that is very easily misued resulting in hard to fix threading problems. If possible use the extension methods that take Func and Action arguments instead")]
        IDisposable LockForExclusiveUse_LowLevelOnlyForBuildingSynchronizationLibraryStyleThingsMethod();
        [Obsolete("This is a low level abstraction that is very easily misued resulting in hard to fix threading problems. If possible use the extension methods that take Func and Action arguments instead")]
        IDisposable LockForExclusiveUse_LowLevelOnlyForBuildingSynchronizationLibraryStyleThingsMethod(TimeSpan timeout);
    }

    static class ObjectLock
    {
        public static IObjectLock WithTimeout(TimeSpan timeout) => new ObjectLockInstance(timeout);

        public static void ExecuteWithExclusiveLock(this IObjectLock @lock, Action action)
        {
            using (@lock.LockForExclusiveUse())
            {
                action();
            }
        }

        public static TResult ExecuteWithExclusiveLock<TResult>(this IObjectLock @lock, Func<TResult> function)
        {
            using (@lock.LockForExclusiveUse())
            {
                return function();
            }
        }

        public static void ExecuteWithExclusiveLock(this IObjectLock @lock, TimeSpan timeout, Action action)
        {
            using (@lock.LockForExclusiveUse(timeout))
            {
                action();
            }
        }

        public static TResult ExecuteWithExclusiveLock<TResult>(this IObjectLock @lock, TimeSpan timeout, Func<TResult> function)
        {
            using (@lock.LockForExclusiveUse(timeout))
            {
                return function();
            }
        }

#pragma warning disable CS0618 // Type or member is obsolete
        static IDisposable LockForExclusiveUse(this IObjectLock @this) => @this.LockForExclusiveUse_LowLevelOnlyForBuildingSynchronizationLibraryStyleThingsMethod();
        static IDisposable LockForExclusiveUse(this IObjectLock @this, TimeSpan timeout) => @this.LockForExclusiveUse_LowLevelOnlyForBuildingSynchronizationLibraryStyleThingsMethod(timeout);
#pragma warning restore CS0618 // Type or member is obsolete

        class ObjectLockInstance : IObjectLock
        {
            readonly object _lockedObject;
            readonly TimeSpan _defaultTimeout;

            public ObjectLockInstance(TimeSpan defaultTimeout)
            {
                _lockedObject = new object();
                _defaultTimeout = defaultTimeout;
            }

            public IDisposable LockForExclusiveUse_LowLevelOnlyForBuildingSynchronizationLibraryStyleThingsMethod() => InternalLockForExclusiveUse(null);
            public IDisposable LockForExclusiveUse_LowLevelOnlyForBuildingSynchronizationLibraryStyleThingsMethod(TimeSpan timeout) => InternalLockForExclusiveUse(timeout);

            IDisposable InternalLockForExclusiveUse(TimeSpan? timeout = null)
            {
                bool lockTaken = false;
                try //It is rare, but apparently possible for Enter to throw an exception after the lock is taken. So we do need to catch it and call Monitor.Exit if that happens.
                {
                    Monitor.TryEnter(_lockedObject, timeout ?? _defaultTimeout, ref lockTaken);

                    if (lockTaken)
                    {
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

                    throw new ObjectLockTimedOutException(_lockedObject);
                }
                catch(Exception)
                {
                    if(lockTaken)
                    {
                        Monitor.Exit(_lockedObject);
                    }
                    throw;
                }
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
