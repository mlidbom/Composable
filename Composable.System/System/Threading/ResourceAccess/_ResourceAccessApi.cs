using System;

namespace Composable.System.Threading.ResourceAccess
{
    interface IExclusiveResourceAccessGuard
    {
        TimeSpan DefaultTimeout { get; }
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
        void ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(TimeSpan? timeoutOverride = null);
    }
}
