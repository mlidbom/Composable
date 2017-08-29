using System;

namespace Composable.System.Threading.ResourceAccess
{
    interface IExclusiveResourceLockManager
    {
        IExclusiveResourceLock AwaitExclusiveLock();
        IExclusiveResourceLock AwaitExclusiveLock(TimeSpan timeout);
    }

    interface IResourceLock : IDisposable {}

    interface IExclusiveResourceLock : IResourceLock
    {
        void ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock();
        void SendUpdateNotificationToOneThreadAwaitingUpdateNotification();
        void SendUpdateNotificationToAllThreadsAwaitingUpdateNotification();
        void ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(TimeSpan timeout);
    }
}
