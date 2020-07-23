using System;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    interface IResourceGuard
    {
        IExclusiveResourceLock AwaitExclusiveLock(TimeSpan? timeoutOverride = null);
        TimeSpan DefaultTimeout { get; }
    }

    interface IResourceLock : IDisposable {}

    interface IResourceUpdateLock : IDisposable
    {
    }

    interface IResourceReadLock : IDisposable
    {
    }

    interface IExclusiveResourceLock : IResourceLock
    {
        void NotifyWaitingThreadsAboutUpdate();

        //todo: These two timeouts are fundamentally different from the timeout waiting to get a lock.
        //Consider a better design. They should probably not share the default timeout designed for lock acquisition.
        void ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(TimeSpan timeout);
        bool TryReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(TimeSpan timeout);
    }
}
