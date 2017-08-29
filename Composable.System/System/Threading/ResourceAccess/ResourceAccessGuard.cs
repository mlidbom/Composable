using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Composable.System.Threading.ResourceAccess
{
    static class ResourceAccessGuard
    {
        public static IExclusiveResourceAccessGuard ExclusiveWithTimeout(TimeSpan timeout) => new ExclusiveResourceAccessGuard(timeout);
        public static ISharedResourceAccessGuard CreateWithMaxSharedLocksAndTimeout(int maxSharedLocks, TimeSpan defaultTimeout) => new SharedResourceAccessGuard(maxSharedLocks, defaultTimeout);

        class ExclusiveResourceAccessGuard : IExclusiveResourceAccessGuard
        {
            readonly List<AwaitingExclusiveResourceLockTimeoutException> _timeOutExceptionsOnOtherThreads = new List<AwaitingExclusiveResourceLockTimeoutException>();
            int _timeoutsThrownDuringCurrentLock;

            readonly object _lockedObject;
            readonly TimeSpan _defaultTimeout;

            public ExclusiveResourceAccessGuard(TimeSpan defaultTimeout)
            {
                _lockedObject = new object();
                _defaultTimeout = defaultTimeout;
            }

            public IExclusiveResourceLock AwaitExclusiveLock(TimeSpan? timeout = null)
            {
                var lockTaken = false;
                try //It is rare, but apparently possible, for TryEnter to throw an exception after the lock is taken. So we do need to catch it and call Monitor.Exit if that happens to avoid leaking locks.
                {
                    Monitor.TryEnter(_lockedObject, timeout ?? _defaultTimeout, ref lockTaken);

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

                    return new ExclusiveResourceLock(this);
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

            class ExclusiveResourceLock : IExclusiveResourceLock
            {
                readonly ExclusiveResourceAccessGuard _parent;
                public ExclusiveResourceLock(ExclusiveResourceAccessGuard parent) { _parent = parent; }
                public void Dispose()
                {
                    try
                    {
                        //Using this exchange trick spares us from taking one more lock every time we release one at the cost of a negligible decrease in the chances for an exception to contain the blocking stacktrace.
                        var timeoutExceptionsOnOtherThreads = Interlocked.Exchange(ref _parent._timeoutsThrownDuringCurrentLock, 0);

                        if(timeoutExceptionsOnOtherThreads > 0)
                        {
                            lock(_parent._timeOutExceptionsOnOtherThreads)
                            {
                                var stackTrace = new StackTrace(fNeedFileInfo: true);
                                foreach (var exception in _parent._timeOutExceptionsOnOtherThreads)
                                {
                                    exception.SetBlockingThreadsDisposeStackTrace(stackTrace);
                                }
                                _parent._timeOutExceptionsOnOtherThreads.Clear();
                            }
                        }
                    }
                    finally
                    {
                        Monitor.Exit(_parent._lockedObject);
                    }
                }

                public void ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(TimeSpan? timeoutOverride = null)
                {
                    if(!Monitor.Wait(_parent._lockedObject, timeoutOverride ?? _parent._defaultTimeout))
                    {
                        throw new AwaitingExclusiveResourceLockTimeoutException();
                    }
                }

                public void SendUpdateNotificationToOneThreadAwaitingUpdateNotification() { Monitor.Pulse(_parent._lockedObject); }

                public void SendUpdateNotificationToAllThreadsAwaitingUpdateNotification() { Monitor.PulseAll(_parent._lockedObject); }
            }
        }

        class SharedResourceAccessGuard : ISharedResourceAccessGuard
        {
            readonly int _maxSharedLocks;
            bool _waitingForExclusiveLock;
            bool _exclusivelyLocked;
            readonly IExclusiveResourceAccessGuard _exclusiveAccessGuard;
            int _currentSharedLocks;

            public SharedResourceAccessGuard(int maxSharedLocks, TimeSpan defaultTimeout)
            {
                _exclusiveAccessGuard = ExclusiveWithTimeout(defaultTimeout);
                _maxSharedLocks = maxSharedLocks;
            }

            public IDisposable AwaitSharedLock(TimeSpan? timeoutOverride = null)
            {
                using(var exclusiveLock = _exclusiveAccessGuard.AwaitExclusiveLock(timeoutOverride))
                {
                    while(_exclusivelyLocked || _waitingForExclusiveLock || _currentSharedLocks == _maxSharedLocks)
                    {
                        exclusiveLock.ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(timeoutOverride);
                    }

                    _currentSharedLocks++;

                    return Disposable.Create(
                        () =>
                        {
                            using(var disposingExclusiveLock =_exclusiveAccessGuard.AwaitExclusiveLock())
                            {
                                _currentSharedLocks--;
                                disposingExclusiveLock.SendUpdateNotificationToOneThreadAwaitingUpdateNotification();
                            }
                        });
                }
            }

            public IExclusiveResourceLock AwaitExclusiveLock(TimeSpan? timeoutOverride = null)
            {
                IExclusiveResourceLock exclusiveLock = null;
                try
                {
                    exclusiveLock = _exclusiveAccessGuard.AwaitExclusiveLock(timeoutOverride);
                    AwaitExclusiveLock(timeoutOverride, exclusiveLock);
                }
                catch (Exception)
                {
                    exclusiveLock?.Dispose();
                    throw;
                }

                return new ExclusiveResourceAccessLockToSharedResource(this, exclusiveLock);
            }

            void AwaitExclusiveLock(TimeSpan? timeoutOverride, IExclusiveResourceLock exclusiveLock)
            {
                _waitingForExclusiveLock = true;
                while(_exclusivelyLocked || _currentSharedLocks != 0)
                {
                    exclusiveLock.ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(timeoutOverride);
                }
                _exclusivelyLocked = true;
                _waitingForExclusiveLock = false;
            }

            class ExclusiveResourceAccessLockToSharedResource : IExclusiveResourceLock
            {
                readonly SharedResourceAccessGuard _parent;
                readonly IExclusiveResourceLock _parentLock;

                public ExclusiveResourceAccessLockToSharedResource(SharedResourceAccessGuard parent, IExclusiveResourceLock parentLock)
                {
                    _parent = parent;
                    _parentLock = parentLock;
                }


                public void SendUpdateNotificationToOneThreadAwaitingUpdateNotification()
                {
                    _parentLock.SendUpdateNotificationToOneThreadAwaitingUpdateNotification();
                }

                public void SendUpdateNotificationToAllThreadsAwaitingUpdateNotification()
                {
                    _parentLock.SendUpdateNotificationToAllThreadsAwaitingUpdateNotification();
                }

                public void ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(TimeSpan? timeoutOverride = null)
                {
                    _parent._exclusivelyLocked = false;
                    _parent.AwaitExclusiveLock(timeoutOverride);
                }

                public void Dispose()
                {
                    using(_parentLock)
                    {
                        _parentLock.SendUpdateNotificationToAllThreadsAwaitingUpdateNotification();
                    }
                }
            }
        }
    }
}
