using System;

namespace Composable.System.Threading.ResourceAccess
{
    interface IGuardedResource
    {
        IExclusiveResourceLock AwaitExclusiveLock(TimeSpan? timeoutOverride = null);
        IResourceReadLock AwaitReadLock(TimeSpan? timeoutOverride = null);
        IResourceUpdateLock AwaitUpdateLock(TimeSpan? timeoutOverride = null);
    }

    interface ISharedGuardedResource : IGuardedResource
    {
        IDisposable AwaitSharedLock(TimeSpan? timeoutOverride = null);
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
        void NotifyASingleWaitingThreadAboutUpdate();
        void NotifyWaitingThreadsAboutUpdate();

        //todo: These two timeouts are fundamentally different from the timeout waiting to get a lock.
        //Consider a better design. They should probably not share the default timeout designed for lock aquisition.
        void ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(TimeSpan timeout);
        bool TryReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(TimeSpan timeout);
    }
}
