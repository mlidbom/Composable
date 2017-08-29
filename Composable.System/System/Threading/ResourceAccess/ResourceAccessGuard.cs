using System;
using System.Threading;

namespace Composable.System.Threading.ResourceAccess
{
    static class ResourceAccessGuard
    {
        public static IExclusiveResourceLockManager WithTimeout(TimeSpan timeout) => new ResourceLockManagerInstance(timeout);

        class ResourceLockManagerInstance : IExclusiveResourceLockManager
        {
            readonly object _lockedObject;
            readonly TimeSpan _defaultTimeout;

            public ResourceLockManagerInstance(TimeSpan defaultTimeout)
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
                        throw new AwaitingExclusiveResourceLockTimeoutException(_lockedObject);
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
                readonly ResourceLockManagerInstance _parent;
                public ExclusiveResourceLock(ResourceLockManagerInstance parent) { _parent = parent; }
                public void Dispose()
                {
                    try
                    {
                        AwaitingExclusiveResourceLockTimeoutException.ReportStackTraceIfError(_parent._lockedObject);
                    }
                    finally
                    {
                        Monitor.Exit(_parent._lockedObject);
                    }
                }

                public void ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(TimeSpan? timeoutOwerride = null)
                {
                    if(!Monitor.Wait(_parent._lockedObject, timeoutOwerride ?? _parent._defaultTimeout))
                    {
                        throw new AwaitingExclusiveResourceLockTimeoutException(_parent._lockedObject);
                    }
                }

                public void SendUpdateNotificationToOneThreadAwaitingUpdateNotification() { Monitor.Pulse(_parent._lockedObject); }

                public void SendUpdateNotificationToAllThreadsAwaitingUpdateNotification() { Monitor.PulseAll(_parent._lockedObject); }
            }
        }
    }
}
