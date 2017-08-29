using System;

namespace Composable.System.Threading.ResourceAccess
{
    interface IExclusiveResourceAccessGuard
    {
        IExclusiveResourceLock AwaitExclusiveLock(TimeSpan? timeoutOverride = null);
    }

    interface ISharedResourceAccessGuard
    {
        IDisposable AwaitSharedLock(TimeSpan? timeoutOverride = null);
        IDisposable AwaitExclusiveLock(TimeSpan? timeoutOverride = null);
    }

    interface IResourceLock : IDisposable {}

    interface IExclusiveResourceLock : IResourceLock
    {
        void SendUpdateNotificationToOneThreadAwaitingUpdateNotification();
        void SendUpdateNotificationToAllThreadsAwaitingUpdateNotification();
        void ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(TimeSpan? timeout = null);
    }
}
