using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    class ResourceGuard : IResourceGuard
    {
        public static IResourceGuard WithTimeout(TimeSpan timeout) => new ResourceGuard(LockCE.WithTimeout(timeout));
        public static IResourceGuard WithDefaultTimeout() => new ResourceGuard(LockCE.WithDefaultTimeout());
        public static IResourceGuard WithInfiniteTimeout() => new ResourceGuard(LockCE.WithInfiniteTimeout());

        readonly List<AcquireLockTimeoutException> _timeOutExceptionsOnOtherThreads = new List<AcquireLockTimeoutException>();
        int _timeoutsThrownDuringCurrentLock;

        readonly LockCE _lock;
        public TimeSpan Timeout => _lock.Timeout;

        ResourceGuard(LockCE @lock) => _lock = @lock;

        public void Await(TimeSpan timeOut, Func<bool> condition) =>
            _lock.AwaitAndAcquire(timeOut, condition);

        public bool TryAwait(TimeSpan timeOut, Func<bool> condition)
        {
            if(_lock.TryAwaitAndAcquire(timeOut, condition))
            {
                _lock.Release();
                return true;
            } else
            {
                return false;
            }
        }

        public TResult Read<TResult>(Func<TResult> read) => WithExclusiveAccess(read, SignalWaitingThreadsMode.None);

        public void Update(Action action) => WithExclusiveAccess(action.AsFunc(), SignalWaitingThreadsMode.All);
        public TResult Update<TResult>(Func<TResult> update) => WithExclusiveAccess(update, SignalWaitingThreadsMode.All);

        public void UpdateWhen(TimeSpan timeout, Func<bool> condition, Action action) =>
            UpdateWhenInternal(timeout, condition, action.AsFunc(), SignalWaitingThreadsMode.All);

        public TResult UpdateWhen<TResult>(TimeSpan timeout, Func<bool> condition, Func<TResult> update) =>
            UpdateWhenInternal(timeout, condition, update, SignalWaitingThreadsMode.All);

        public IResourceLock AwaitUpdateLockWhen(TimeSpan timeout, Func<bool> condition) =>
            AwaitExclusiveLockWhenInternal(timeout, condition, SignalWaitingThreadsMode.All);

        public IResourceLock AwaitUpdateLock(TimeSpan? timeout = null) =>
            AwaitExclusiveLockInternal(timeout, SignalWaitingThreadsMode.All);

        ResourceLock AwaitExclusiveLockWhenInternal(TimeSpan timeout, Func<bool> condition, SignalWaitingThreadsMode signalWaitingThreadsMode)
        {
            _lock.AwaitAndAcquire(timeout, condition);
            return new ResourceLock(this, signalWaitingThreadsMode);
        }

        TResult UpdateWhenInternal<TResult>(TimeSpan timeout, Func<bool> condition, Func<TResult> func, SignalWaitingThreadsMode signalWaitingThreadsMode)
        {
            _lock.AwaitAndAcquire(timeout, condition);
            try
            {
                return func();
            }
            finally
            {
                _lock.SignalWaitingThreadsAndRelease(signalWaitingThreadsMode);
            }
        }

        void AcquireLock(TimeSpan? timeout)
        {
            if(!_lock.TryAcquire(timeout))
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
            readonly ResourceGuard _guard;
            readonly SignalWaitingThreadsMode _signalWaitingThreadsMode;
            public ResourceLock(ResourceGuard guard, SignalWaitingThreadsMode signalWaitingThreadsMode)
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
