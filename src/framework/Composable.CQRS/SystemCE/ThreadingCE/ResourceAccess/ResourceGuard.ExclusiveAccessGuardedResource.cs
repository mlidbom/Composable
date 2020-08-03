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
            readonly List<AcquireLockTimeoutException> _timeOutExceptionsOnOtherThreads = new List<AcquireLockTimeoutException>();
            int _timeoutsThrownDuringCurrentLock;

            readonly LockCE _lock;
            public TimeSpan DefaultTimeout { get; private set; }

            public ExclusiveAccessResourceGuard(TimeSpan defaultTimeout)
            {
                _lock = LockCE.WithTimeout(defaultTimeout);
                DefaultTimeout = defaultTimeout;
            }

            public TResult Read<TResult>(Func<TResult> read) => WithExclusiveAccess(read, SignalWaitingThreadsMode.None);

            public void Update(Action action) => WithExclusiveAccess(action.AsFunc(), SignalWaitingThreadsMode.All);
            public TResult Update<TResult>(Func<TResult> update) => WithExclusiveAccess(update, SignalWaitingThreadsMode.All);

            public IResourceLock AwaitReadLockWhen(TimeSpan timeout, Func<bool> condition) =>
                AwaitExclusiveLockWhenInternal(timeout, condition, SignalWaitingThreadsMode.None);

            public IResourceLock AwaitReadLock(TimeSpan? timeout = null) =>
                AwaitExclusiveLockInternal(timeout, SignalWaitingThreadsMode.None);

            public IResourceLock AwaitUpdateLockWhen(TimeSpan timeout, Func<bool> condition) =>
                AwaitExclusiveLockWhenInternal(timeout, condition, SignalWaitingThreadsMode.All);

            public IResourceLock AwaitUpdateLock(TimeSpan? timeout = null) =>
                AwaitExclusiveLockInternal(timeout, SignalWaitingThreadsMode.All);

            ResourceLock AwaitExclusiveLockWhenInternal(TimeSpan timeout, Func<bool> condition, SignalWaitingThreadsMode signalWaitingThreadsMode)
            {
                _lock.AwaitAndAcquire(timeout, condition);
                return new ResourceLock(this, signalWaitingThreadsMode);
            }

            void AcquireLock(TimeSpan? timeout)
            {
                if(!_lock.TryAcquireLock(timeout ?? DefaultTimeout))
                {
                    lock(_timeOutExceptionsOnOtherThreads)
                    {
                        Interlocked.Increment(ref _timeoutsThrownDuringCurrentLock);
                        var exception = new AcquireLockTimeoutException();
                        _timeOutExceptionsOnOtherThreads.Add(exception);
                        throw exception;
                    }
                }
            }

            ResourceLock AwaitExclusiveLockInternal(TimeSpan? timeout, SignalWaitingThreadsMode signalWaitingThreadsMode)
            {
                AcquireLock(timeout);
                return new ResourceLock(this, signalWaitingThreadsMode);
            }

            TResult WithExclusiveAccess<TResult>(Func<TResult> func, SignalWaitingThreadsMode signalMode)
            {
                AcquireLock(null);
                try
                {
                    return func();
                }
                finally
                {
                    _lock.SignalWaitingThreadsAndRelease(signalMode);
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

            class ResourceLock : IResourceLock
            {
                readonly ExclusiveAccessResourceGuard _guard;
                readonly SignalWaitingThreadsMode _signalWaitingThreadsMode;
                public ResourceLock(ExclusiveAccessResourceGuard guard, SignalWaitingThreadsMode signalWaitingThreadsMode)
                {
                    _guard = guard;
                    _signalWaitingThreadsMode = signalWaitingThreadsMode;
                }

                public void Dispose()
                {
                    try
                    {
                        _guard.SetBlockingStackTraceInTimedOutExceptions();
                    }
                    finally
                    {
                        _guard._lock.SignalWaitingThreadsAndRelease(_signalWaitingThreadsMode);
                    }
                }
            }
        }
    }
}
