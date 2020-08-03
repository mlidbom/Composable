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
            readonly List<AwaitingResourceLockTimeoutException> _timeOutExceptionsOnOtherThreads = new List<AwaitingResourceLockTimeoutException>();
            int _timeoutsThrownDuringCurrentLock;

            readonly object _lockedObject;
            public TimeSpan DefaultTimeout { get; private set; }

            public ExclusiveAccessResourceGuard(TimeSpan defaultTimeout)
            {
                _lockedObject = new object();
                DefaultTimeout = defaultTimeout;
            }

            public TResult Read<TResult>(Func<TResult> read) => WithExclusiveAccess(read, PulseMode.None);

            public void Update(Action action) => WithExclusiveAccess(action.AsFunc(), PulseMode.All);
            public TResult Update<TResult>(Func<TResult> update) => WithExclusiveAccess(update, PulseMode.All);

            public IResourceLock AwaitReadLockWhen(TimeSpan timeout, Func<bool> condition) =>
                AwaitExclusiveLockWhenInternal(timeout, condition, PulseMode.None);

            public IResourceLock AwaitReadLock(TimeSpan? timeout = null) =>
                AwaitExclusiveLockInternal(timeout, PulseMode.None);

            public IResourceLock AwaitUpdateLockWhen(TimeSpan timeout, Func<bool> condition) =>
                AwaitExclusiveLockWhenInternal(timeout, condition, PulseMode.All);

            public IResourceLock AwaitUpdateLock(TimeSpan? timeout = null) =>
                AwaitExclusiveLockInternal(timeout, PulseMode.All);

            int _waitingThreadCount = 0;
            ResourceLock AwaitExclusiveLockWhenInternal(TimeSpan timeout, Func<bool> condition, PulseMode pulseMode)
            {
                try
                {
                    Interlocked.Increment(ref _waitingThreadCount);
                    var startTime = DateTime.UtcNow;

                    TakeLock(timeout);
                    while(!condition())
                    {
                        var elapsedTime = DateTime.UtcNow - startTime;
                        var timeRemaining = timeout - elapsedTime;
                        if(elapsedTime > timeout)
                        {
                            Monitor.Exit(_lockedObject);
                            throw new AwaitingConditionTimedOutException();
                        }

                        ReleaseLockWaitForPulseOrTimeoutAndReacquireLock(timeRemaining);
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref _waitingThreadCount);
                }

                return new ResourceLock(this, pulseMode);
            }

            void ReleaseLockWaitForPulseOrTimeoutAndReacquireLock(TimeSpan timeRemaining) => Monitor.Wait(_lockedObject, timeRemaining);

            ResourceLock AwaitExclusiveLockInternal(TimeSpan? timeout, PulseMode pulseMode)
            {
                TakeLock(timeout);
                return new ResourceLock(this, pulseMode);
            }

            TResult WithExclusiveAccess<TResult>(Func<TResult> func, PulseMode pulseMode)
            {
                TakeLock(null);
                try
                {
                    return func();
                }
                finally
                {
                    Pulse(pulseMode);
                    Monitor.Exit(_lockedObject);
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
                if(_waitingThreadCount == 0)
                {
                    return; //Pulsing is relatively expensive. Let's avoid it when we can and then Update and UpdateLocks can be used without hesitation about performance.
                }

                switch(pulseMode)
                {
                    case PulseMode.None:
                        break;
                    case PulseMode.One:
                        Monitor.Pulse(_lockedObject); //One thread blocked on Monitor.Wait for our _lockedObject, if there is such a thread, will now try and reacquire the lock.
                        break;
                    case PulseMode.All:
                        Monitor.PulseAll(_lockedObject); //All threads blocked on Monitor.Wait for our _lockedObject, if there are such threads, will now try and reacquire the lock.
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

            void TakeLock(TimeSpan? timeout)
            {
                var lockTaken = false;
                try
                {
                    Monitor.TryEnter(_lockedObject, timeout ?? DefaultTimeout, ref lockTaken);

                    if(!lockTaken)
                    {
                        lock(_timeOutExceptionsOnOtherThreads)
                        {
                            Interlocked.Increment(ref _timeoutsThrownDuringCurrentLock);
                            var exception = new AwaitingResourceLockTimeoutException();
                            _timeOutExceptionsOnOtherThreads.Add(exception);
                            throw exception;
                        }
                    }
                }
                catch(Exception) //It is rare, but apparently possible, for TryEnter to throw an exception after the lock is taken. So we do need to catch it and call Monitor.Exit if that happens to avoid leaking locks.
                {
                    if(lockTaken) Monitor.Exit(_lockedObject);
                    throw;
                }
            }

            class ResourceLock : IResourceLock
            {
                readonly ExclusiveAccessResourceGuard _guard;
                readonly PulseMode _pulseMode;
                public ResourceLock(ExclusiveAccessResourceGuard guard, PulseMode pulseMode)
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
            }
        }
    }
}
