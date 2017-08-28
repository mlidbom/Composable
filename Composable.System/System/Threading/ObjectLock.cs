using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Composable.System.Threading
{
    interface IObjectLock
    {
        IObjectLockOwner LockForExclusiveUse();
        IObjectLockOwner LockForExclusiveUse(TimeSpan timeout);
    }

    interface IObjectLockOwner : IDisposable
    {
        void Wait();
        void Pulse();
        void PulseAll();
        void Wait(TimeSpan timeout);
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
            using (@lock.LockForExclusiveUse())
            {
                action();
            }
        }

        public static TResult ExecuteWithExclusiveLock<TResult>(this IObjectLock @lock, TimeSpan timeout, Func<TResult> function)
        {
            using (@lock.LockForExclusiveUse())
            {
                return function();
            }
        }

        class ObjectLockInstance : IObjectLock
        {
            readonly object _lockedObject;
            readonly TimeSpan _defaultTimeout;

            public ObjectLockInstance(TimeSpan defaultTimeout)
            {
                _lockedObject = new object();
                _defaultTimeout = defaultTimeout;
            }

            public IObjectLockOwner LockForExclusiveUse() => InternalLockForExclusiveUse(null);
            public IObjectLockOwner LockForExclusiveUse(TimeSpan timeout) => InternalLockForExclusiveUse(timeout);

            IObjectLockOwner InternalLockForExclusiveUse(TimeSpan? timeout = null)
            {
                bool lockTaken = false;
                try //It is rare, but apparently possible for Enter to throw an exception after the lock is taken. So we do need to catch it and call Monitor.Exit if that happens.
                {
                    Monitor.TryEnter(_lockedObject, timeout ?? _defaultTimeout, ref lockTaken);

                    if (lockTaken)
                    {
                        return new ObjectLockOwner(this);
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

            class ObjectLockOwner : IObjectLockOwner
            {
                readonly ObjectLockInstance _parent;
                public ObjectLockOwner(ObjectLockInstance parent) { _parent = parent; }
                public void Dispose()
                {
                    try
                    {
                        ObjectLockTimedOutException.ReportStackTraceIfError(_parent._lockedObject);
                    }
                    finally
                    {
                        Monitor.Exit(_parent._lockedObject);
                    }
                }

                public void Wait()
                {
                    if(!Monitor.Wait(_parent._lockedObject, _parent._defaultTimeout))
                    {
                        throw new ObjectLockTimedOutException(_parent._lockedObject);
                    }
                }

                public void Wait(TimeSpan timeout)
                {
                    if (!Monitor.Wait(_parent._lockedObject, timeout))
                    {
                        throw new ObjectLockTimedOutException(_parent._lockedObject);
                    }
                }

                public void Pulse()
                {
                    Monitor.Pulse(_parent._lockedObject);
                }

                public void PulseAll()
                {
                    Monitor.PulseAll(_parent._lockedObject);
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

        internal ObjectLockTimedOutException(object lockedObject) : base("_defaultTimeout waiting for lock")
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
