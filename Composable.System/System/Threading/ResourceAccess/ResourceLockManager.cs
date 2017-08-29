using System;
using System.Threading;

namespace Composable.System.Threading.ResourceAccess
{
    static class ResourceLockManager
    {
        public static IExclusiveResourceLockManager WithTimeout(TimeSpan timeout) => new ResourceLockManagerInstance(timeout);

        public static void ExecuteWithExclusiveLock(this IExclusiveResourceLockManager @lock, Action action)
        {
            using (@lock.AwaitExclusiveLock())
            {
                action();
            }
        }

        public static TResult ExecuteWithExclusiveLock<TResult>(this IExclusiveResourceLockManager @lock, Func<TResult> function)
        {
            using (@lock.AwaitExclusiveLock())
            {
                return function();
            }
        }

        public static void ExecuteWithExclusiveLock(this IExclusiveResourceLockManager @lock, TimeSpan timeout, Action action)
        {
            using (@lock.AwaitExclusiveLock())
            {
                action();
            }
        }

        public static TResult ExecuteWithExclusiveLock<TResult>(this IExclusiveResourceLockManager @lock, TimeSpan timeout, Func<TResult> function)
        {
            using (@lock.AwaitExclusiveLock())
            {
                return function();
            }
        }

        class ResourceLockManagerInstance : IExclusiveResourceLockManager
        {
            readonly object _lockedObject;
            readonly TimeSpan _defaultTimeout;

            public ResourceLockManagerInstance(TimeSpan defaultTimeout)
            {
                _lockedObject = new object();
                _defaultTimeout = defaultTimeout;
            }

            public IExclusiveResourceLock AwaitExclusiveLock() => InternalLockForExclusiveUse(null);
            public IExclusiveResourceLock AwaitExclusiveLock(TimeSpan timeout) => InternalLockForExclusiveUse(timeout);

            IExclusiveResourceLock InternalLockForExclusiveUse(TimeSpan? timeout = null)
            {
                bool lockTaken = false;
                try //It is rare, but apparently possible for Enter to throw an exception after the lock is taken. So we do need to catch it and call Monitor.Exit if that happens.
                {
                    Monitor.TryEnter(_lockedObject, timeout ?? _defaultTimeout, ref lockTaken);

                    if (lockTaken)
                    {
                        return new ExclusiveResourceLock(this);
                    }

                    throw new AwaitingExclusiveResourcAccessLeaseTimeoutException(_lockedObject);
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
                        AwaitingExclusiveResourcAccessLeaseTimeoutException.ReportStackTraceIfError(_parent._lockedObject);
                    }
                    finally
                    {
                        Monitor.Exit(_parent._lockedObject);
                    }
                }

                public void ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock()
                {
                    if(!Monitor.Wait(_parent._lockedObject, _parent._defaultTimeout))
                    {
                        throw new AwaitingExclusiveResourcAccessLeaseTimeoutException(_parent._lockedObject);
                    }
                }

                public void ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(TimeSpan timeout)
                {
                    if (!Monitor.Wait(_parent._lockedObject, timeout))
                    {
                        throw new AwaitingExclusiveResourcAccessLeaseTimeoutException(_parent._lockedObject);
                    }
                }

                public void SendUpdateNotificationToOneThreadAwaitingUpdateNotification()
                {
                    Monitor.Pulse(_parent._lockedObject);
                }

                public void SendUpdateNotificationToAllThreadsAwaitingUpdateNotification()
                {
                    Monitor.PulseAll(_parent._lockedObject);
                }
            }
        }
    }
}
