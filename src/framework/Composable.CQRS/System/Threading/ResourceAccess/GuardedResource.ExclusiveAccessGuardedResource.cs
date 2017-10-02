using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Composable.System.Threading.ResourceAccess
{
    static partial class GuardedResource
    {
        class ExclusiveAccessGuardedResource : IGuardedResource
        {
            readonly List<AwaitingExclusiveResourceLockTimeoutException> _timeOutExceptionsOnOtherThreads = new List<AwaitingExclusiveResourceLockTimeoutException>();
            int _timeoutsThrownDuringCurrentLock;

            readonly object _lockedObject;
            readonly TimeSpan _defaultTimeout;

            public ExclusiveAccessGuardedResource(TimeSpan defaultTimeout)
            {
                _lockedObject = new object();
                _defaultTimeout = defaultTimeout;
            }

            public IResourceReadLock AwaitReadLock(TimeSpan? timeoutOverride = null)
                => new ReadResourceLock(AwaitExclusiveLock(timeoutOverride));

            public IResourceUpdateLock AwaitUpdateLock(TimeSpan? timeoutOverride = null)
                => new UpdateResourceLock(AwaitExclusiveLock(timeoutOverride));

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

            class ReadResourceLock : IResourceReadLock
            {
                readonly IExclusiveResourceLock _lock;
                public ReadResourceLock(IExclusiveResourceLock @lock) => _lock = @lock;

                public void Dispose() { _lock.Dispose(); }
            }

            class UpdateResourceLock : IResourceUpdateLock
            {
                readonly IExclusiveResourceLock _lock;
                public UpdateResourceLock(IExclusiveResourceLock @lock) => _lock = @lock;

                public void Dispose()
                {
                    _lock.NotifyWaitingThreadsAboutUpdate();
                    _lock.Dispose();
                }
            }

            class ExclusiveResourceLock : IExclusiveResourceLock
            {
                readonly ExclusiveAccessGuardedResource _parent;
                public ExclusiveResourceLock(ExclusiveAccessGuardedResource parent) => _parent = parent;
                public void Dispose()
                {
                    try
                    {
                        //todo: Log a warning if disposing after a longer time than the default lock timeout.
                        //Using this exchange trick spares us from taking one more lock every time we release one at the cost of a negligible decrease in the chances for an exception to contain the blocking stacktrace.
                        var timeoutExceptionsOnOtherThreads = Interlocked.Exchange(ref _parent._timeoutsThrownDuringCurrentLock, 0);

                        if(timeoutExceptionsOnOtherThreads > 0)
                        {
                            lock(_parent._timeOutExceptionsOnOtherThreads)
                            {
                                var stackTrace = new StackTrace(fNeedFileInfo: true);
                                foreach(var exception in _parent._timeOutExceptionsOnOtherThreads)
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

                public bool TryReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(TimeSpan timeout)
                {
                    if(!Monitor.Wait(_parent._lockedObject, timeout))
                    {
                        return false;
                    }
                    return true;
                }

                public void ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(TimeSpan timeout)
                {
                    if(!Monitor.Wait(_parent._lockedObject, timeout))
                    {
                        throw new AwaitingUpdateNotificationTimedOutException();
                    }
                }

                public void NotifyWaitingThreadsAboutUpdate() { Monitor.PulseAll(_parent._lockedObject); }
            }
        }
    }
}
