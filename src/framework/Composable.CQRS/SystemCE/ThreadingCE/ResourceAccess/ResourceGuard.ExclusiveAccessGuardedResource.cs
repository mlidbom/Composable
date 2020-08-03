using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    static partial class ResourceGuard
    {
        class ExclusiveAccessResourceGuard : IResourceGuard
        {
            readonly List<AwaitingExclusiveResourceLockTimeoutException> _timeOutExceptionsOnOtherThreads = new List<AwaitingExclusiveResourceLockTimeoutException>();
            int _timeoutsThrownDuringCurrentLock;

            readonly object _lockedObject;
            public TimeSpan DefaultTimeout { get; private set; }

            public ExclusiveAccessResourceGuard(TimeSpan defaultTimeout)
            {
                _lockedObject = new object();
                DefaultTimeout = defaultTimeout;
            }

            public void WithExclusiveAccess(Action action) => WithExclusiveAccessInternal(action.AsFunc(), PulseMode.None);
            public TResult WithExclusiveAccess<TResult>(Func<TResult> func) => WithExclusiveAccessInternal(func, PulseMode.None);

            public TResult Read<TResult>(Func<TResult> read) => WithExclusiveAccessInternal(read, PulseMode.None);

            public void Update(Action action) => WithExclusiveAccessInternal(action.AsFunc(), PulseMode.All);
            public TResult Update<TResult>(Func<TResult> update) => WithExclusiveAccessInternal(update, PulseMode.All);

            public IResourceReadLock AwaitReadLockWhen(TimeSpan timeout, Func<bool> condition) =>
                AwaitExclusiveLockWhenInternal(timeout, condition, PulseMode.None);

            public IResourceReadLock AwaitReadLock(TimeSpan? timeout = null) =>
                AwaitExclusiveLockInternal(timeout, PulseMode.None);

            public IResourceUpdateLock AwaitUpdateLockWhen(TimeSpan timeout, Func<bool> condition) =>
                AwaitExclusiveLockWhenInternal(timeout, condition, PulseMode.All);

            public IResourceUpdateLock AwaitUpdateLock(TimeSpan? timeout = null) =>
                AwaitExclusiveLockInternal(timeout, PulseMode.All);

            public IExclusiveResourceLock AwaitExclusiveLockWhen(TimeSpan timeout, Func<bool> condition) =>
                AwaitExclusiveLockWhenInternal(timeout, condition, PulseMode.None);

            public IExclusiveResourceLock AwaitExclusiveLock(TimeSpan? timeout = null) =>
                AwaitExclusiveLockInternal(timeout, PulseMode.None);

            ExclusiveResourceLock AwaitExclusiveLockWhenInternal(TimeSpan timeout, Func<bool> condition, PulseMode pulseMode)
            {
                var lockTaken = false;
                var startTime = DateTime.UtcNow;

                TryTakeLock(timeout, ref lockTaken);
                while(!condition())
                {
                    if(DateTime.UtcNow - startTime > timeout)
                    {
                        Monitor.Exit(_lockedObject);
                        throw new AwaitingConditionTimedOutException();
                    }

                    if(!Monitor.Wait(_lockedObject, timeout))
                    {
                        throw new AwaitingConditionTimedOutException();
                    }
                }

                return new ExclusiveResourceLock(this, pulseMode);
            }

            ExclusiveResourceLock AwaitExclusiveLockInternal(TimeSpan? timeout, PulseMode pulseMode)
            {
                var lockTaken = false;
                TryTakeLock(timeout, ref lockTaken);
                return new ExclusiveResourceLock(this, pulseMode);
            }

            TResult WithExclusiveAccessInternal<TResult>(Func<TResult> func, PulseMode pulseMode)
            {
                var lockTaken = false;
                try
                {
                    TryTakeLock(null, ref lockTaken);
                    return func();
                }
                finally
                {
                    if(lockTaken)
                    {
                        Pulse(pulseMode);
                        Monitor.Exit(_lockedObject);
                    }
                }
            }


            enum PulseMode
            {
                None,
                One,
                All
            }

            void Pulse(PulseMode pulseMode)
            {
                switch(pulseMode)
                {
                    case PulseMode.None:
                        break;
                    case PulseMode.One:
                        Monitor.Pulse(_lockedObject);
                        break;
                    case PulseMode.All:
                        Monitor.PulseAll(_lockedObject);
                        break;
                }
            }

            void SetBlockingStackTraceInTimedOutExceptions()
            {
                //todo: Log a warning if disposing after a longer time than the default lock timeout.
                //Using this exchange trick spares us from taking one more lock every time we release one at the cost of a negligible decrease in the chances for an exception to contain the blocking stacktrace.
                var timeoutExceptionsOnOtherThreads = Interlocked.Exchange(ref _timeoutsThrownDuringCurrentLock, 0);

                if(timeoutExceptionsOnOtherThreads > 0)
                {
                    lock(_timeOutExceptionsOnOtherThreads)
                    {
                        var stackTrace = new StackTrace(fNeedFileInfo: true);
                        foreach(var exception in _timeOutExceptionsOnOtherThreads)
                        {
                            exception.SetBlockingThreadsDisposeStackTrace(stackTrace);
                        }

                        _timeOutExceptionsOnOtherThreads.Clear();
                    }
                }
            }

            void TryTakeLock(TimeSpan? timeout, ref bool lockTaken)
            {
                try //It is rare, but apparently possible, for TryEnter to throw an exception after the lock is taken. So we do need to catch it and call Monitor.Exit if that happens to avoid leaking locks.
                {
                    Monitor.TryEnter(_lockedObject, timeout ?? DefaultTimeout, ref lockTaken);

                    if(!lockTaken)
                    {
                        lock(_timeOutExceptionsOnOtherThreads)
                        {
                            Interlocked.Increment(ref _timeoutsThrownDuringCurrentLock);
                            var exception = new AwaitingExclusiveResourceLockTimeoutException();
                            _timeOutExceptionsOnOtherThreads.Add(exception);
                            throw exception;
                        }
                    }
                }
                catch(Exception)
                {
                    if(lockTaken) Monitor.Exit(_lockedObject);
                    throw;
                }
            }

            class ExclusiveResourceLock : IExclusiveResourceLock, IResourceUpdateLock, IResourceReadLock
            {
                readonly ExclusiveAccessResourceGuard _guard;
                readonly PulseMode _pulseMode;
                public ExclusiveResourceLock(ExclusiveAccessResourceGuard guard, PulseMode pulseMode)
                {
                    _guard = guard;
                    _pulseMode = pulseMode;
                }
                public void Dispose()
                {
                    try
                    {
                        _guard.SetBlockingStackTraceInTimedOutExceptions();
                    }
                    finally
                    {
                        _guard.Pulse(_pulseMode);
                        Monitor.Exit(_guard._lockedObject);
                    }
                }

                public bool TryReleaseAwaitNotificationAndReacquire(TimeSpan timeout) =>
                    Monitor.Wait(_guard._lockedObject, timeout);

                public void ReleaseAwaitNotificationAndReacquire(TimeSpan timeout)
                {
                    if(!TryReleaseAwaitNotificationAndReacquire(timeout))
                        throw new AwaitingUpdateNotificationTimedOutException();
                }

                public void NotifyAllWaitingThreads() => Monitor.PulseAll(_guard._lockedObject);

                public void NotifyOneWaitingThread() => Monitor.Pulse(_guard._lockedObject);
            }
        }
    }
}
