using System;

namespace Composable.System.Threading.ResourceAccess
{
    interface IExclusiveResourceAccessGuard
    {
        IExclusiveResourceLock AwaitExclusiveLock(TimeSpan? timeoutOverride = null);
    }

    interface ISharedResourceAccessGuard : IExclusiveResourceAccessGuard
    {
        IDisposable AwaitSharedLock(TimeSpan? timeoutOverride = null);
    }

    interface IResourceLock : IDisposable {}

    interface IExclusiveResourceLock : IResourceLock
    {
        void SendUpdateNotificationToOneThreadAwaitingUpdateNotification();
        void SendUpdateNotificationToAllThreadsAwaitingUpdateNotification();

        //todo: These two timeouts are fundamentally different from the timeout waiting to get a lock.
        //Consider a better design. They should probably not share the default timeout designed for lock aquisition.
        void ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(TimeSpan timeout);
        bool TryReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(TimeSpan timeout);
    }
}
