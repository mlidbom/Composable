using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Composable.Testing.System.Threading.ResourceAccess
{
    static partial class ResourceAccessGuard
    {
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

                    if (!lockTaken)
                    {
                        lock (_timeOutExceptionsOnOtherThreads)
                        {
                            Interlocked.Increment(ref _timeoutsThrownDuringCurrentLock);
                            var exception = new AwaitingExclusiveResourceLockTimeoutException();
                            _timeOutExceptionsOnOtherThreads.Add(exception);
                            throw exception;
                        }
                    }

                    return new ExclusiveResourceLock(this);
                }
                catch (Exception)
                {
                    if (lockTaken)
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

                        if (timeoutExceptionsOnOtherThreads > 0)
                        {
                            lock (_parent._timeOutExceptionsOnOtherThreads)
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

                public bool TryReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(TimeSpan? timeoutOverride = null)
                {
                    if (!Monitor.Wait(_parent._lockedObject, timeoutOverride ?? _parent._defaultTimeout))
                    {
                        return false;
                    }
                    return true;
                }

                public void ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(TimeSpan? timeoutOverride = null)
                {
                    if (!Monitor.Wait(_parent._lockedObject, timeoutOverride ?? _parent._defaultTimeout))
                    {
                        throw new AwaitingUpdateNotificationTimedOutException();
                    }
                }

                public void SendUpdateNotificationToOneThreadAwaitingUpdateNotification() { Monitor.Pulse(_parent._lockedObject); }

                public void SendUpdateNotificationToAllThreadsAwaitingUpdateNotification() { Monitor.PulseAll(_parent._lockedObject); }
            }
        }
    }
}
