using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    //Urgent: The split between this and LockCE does not seem to really make sense.
    class ResourceGuard : IResourceGuard
    {
        public static IResourceGuard WithTimeout(TimeSpan timeout) => new ResourceGuard(MonitorCE.WithTimeout(timeout));
        public static IResourceGuard WithDefaultTimeout() => new ResourceGuard(MonitorCE.WithDefaultTimeout());
        public static IResourceGuard WithInfiniteTimeout() => new ResourceGuard(MonitorCE.WithInfiniteTimeout());

        readonly List<AcquireLockTimeoutException> _timeOutExceptionsOnOtherThreads = new List<AcquireLockTimeoutException>();
        int _timeoutsThrownDuringCurrentLock;

        readonly MonitorCE _monitor;
        ResourceGuard(MonitorCE monitor) => _monitor = monitor;
        public TimeSpan Timeout => _monitor.Timeout;

        public void Await(TimeSpan conditionTimeout, Func<bool> condition) =>
            _monitor.EnterWhen(conditionTimeout, condition);

        public bool TryAwait(TimeSpan conditionTimeout, Func<bool> condition)
        {
            if(_monitor.TryEnterWhen(conditionTimeout, condition))
            {
                _monitor.Exit();
                return true;
            } else
            {
                return false;
            }
        }

        public TResult Read<TResult>(Func<TResult> read) => EnterDoNotifyExit(read, NotifyWaiting.None);

        public void Update(Action action) => EnterDoNotifyExit(action.AsFunc(), NotifyWaiting.All);
        public TResult Update<TResult>(Func<TResult> update) => EnterDoNotifyExit(update, NotifyWaiting.All);

        public void UpdateWhen(TimeSpan timeout, Func<bool> condition, Action action) =>
            UpdateWhenInternal(timeout, condition, action.AsFunc(), NotifyWaiting.All);

        public TResult UpdateWhen<TResult>(TimeSpan timeout, Func<bool> condition, Func<TResult> update) =>
            UpdateWhenInternal(timeout, condition, update, NotifyWaiting.All);

        public IResourceLock AwaitUpdateLockWhen(TimeSpan conditionTimeout, Func<bool> condition) =>
            AwaitExclusiveLockWhenInternal(conditionTimeout, condition, NotifyWaiting.All);

        public IResourceLock AwaitUpdateLock(TimeSpan timeout) =>
            AwaitExclusiveLockInternal(timeout, NotifyWaiting.All);

        ResourceLock AwaitExclusiveLockWhenInternal(TimeSpan conditionTimeout, Func<bool> condition, NotifyWaiting notifyWaiting)
        {
            _monitor.EnterWhen(conditionTimeout, condition);
            return new ResourceLock(this, notifyWaiting);
        }

        TResult UpdateWhenInternal<TResult>(TimeSpan conditionTimeout, Func<bool> condition, Func<TResult> func, NotifyWaiting notifyWaiting)
        {
            _monitor.EnterWhen(conditionTimeout, condition);
            try
            {
                return func();
            }
            finally
            {
                _monitor.SignalAndExit(notifyWaiting);
            }
        }

        void AcquireLock(TimeSpan? timeout)
        {
            if(!_monitor.TryEnter(timeout))
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

        ResourceLock AwaitExclusiveLockInternal(TimeSpan? timeout, NotifyWaiting notifyWaiting)
        {
            AcquireLock(timeout);
            return new ResourceLock(this, notifyWaiting);
        }

        TResult EnterDoNotifyExit<TResult>(Func<TResult> func, NotifyWaiting notifyMode)
        {
            AcquireLock(null);
            try
            {
                return func();
            }
            finally
            {
                _monitor.SignalAndExit(notifyMode);
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
            readonly NotifyWaiting _notifyWaiting;
            public ResourceLock(ResourceGuard guard, NotifyWaiting notifyWaiting)
            {
                _guard = guard;
                _notifyWaiting = notifyWaiting;
            }

            public void Dispose()
            {
                try
                {
                    _guard.SetBlockingStackTraceInTimedOutExceptions();
                }
                finally
                {
                    _guard._monitor.SignalAndExit(_notifyWaiting);
                }
            }
        }
    }
}
