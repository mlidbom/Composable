using System;

namespace Composable.System.Threading.ResourceAccess
{
    static partial class ResourceAccessGuard
    {
        class SharedResourceAccessGuard : ISharedResourceAccessGuard
        {
            readonly int _maxSharedLocks;
            bool _waitingForExclusiveLock;
            bool _exclusivelyLocked;
            readonly IExclusiveResourceAccessGuard _exclusiveAccessGuard;
            int _currentSharedLocks;

            public SharedResourceAccessGuard(int maxSharedLocks, TimeSpan defaultTimeout)
            {
                _exclusiveAccessGuard = ExclusiveWithTimeout(defaultTimeout);
                _maxSharedLocks = maxSharedLocks;
            }

            public IDisposable AwaitSharedLock(TimeSpan? timeoutOverride = null)
            {
                using (var exclusiveLock = _exclusiveAccessGuard.AwaitExclusiveLock(timeoutOverride))
                {
                    while (_exclusivelyLocked || _waitingForExclusiveLock || _currentSharedLocks == _maxSharedLocks)
                    {
                        exclusiveLock.ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(timeoutOverride);
                    }

                    _currentSharedLocks++;

                    return Disposable.Create(
                        () =>
                        {
                            using (var disposingExclusiveLock = _exclusiveAccessGuard.AwaitExclusiveLock())
                            {
                                _currentSharedLocks--;
                                disposingExclusiveLock.SendUpdateNotificationToOneThreadAwaitingUpdateNotification();
                            }
                        });
                }
            }

            public IExclusiveResourceLock AwaitExclusiveLock(TimeSpan? timeoutOverride = null)
            {
                IExclusiveResourceLock exclusiveLock = null;
                try
                {
                    exclusiveLock = _exclusiveAccessGuard.AwaitExclusiveLock(timeoutOverride);
                    AwaitExclusiveLock(timeoutOverride, exclusiveLock);
                }
                catch (Exception)
                {
                    exclusiveLock?.Dispose();
                    throw;
                }

                return new ExclusiveResourceAccessLockToSharedResource(this, exclusiveLock);
            }

            void AwaitExclusiveLock(TimeSpan? timeoutOverride, IExclusiveResourceLock exclusiveLock)
            {
                _waitingForExclusiveLock = true;
                while (_exclusivelyLocked || _currentSharedLocks != 0)
                {
                    exclusiveLock.ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(timeoutOverride);
                }
                _exclusivelyLocked = true;
                _waitingForExclusiveLock = false;
            }

            class ExclusiveResourceAccessLockToSharedResource : IExclusiveResourceLock
            {
                readonly SharedResourceAccessGuard _parent;
                readonly IExclusiveResourceLock _parentLock;

                public ExclusiveResourceAccessLockToSharedResource(SharedResourceAccessGuard parent, IExclusiveResourceLock parentLock)
                {
                    _parent = parent;
                    _parentLock = parentLock;
                }

                public void SendUpdateNotificationToOneThreadAwaitingUpdateNotification() => _parentLock.SendUpdateNotificationToOneThreadAwaitingUpdateNotification();

                public void SendUpdateNotificationToAllThreadsAwaitingUpdateNotification() => _parentLock.SendUpdateNotificationToAllThreadsAwaitingUpdateNotification();

                public void ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(TimeSpan? timeoutOverride = null)
                {
                    _parent._exclusivelyLocked = false;
                    _parent.AwaitExclusiveLock(timeoutOverride);
                }

                public void Dispose()
                {
                    using (_parentLock)
                    {
                        _parent._exclusivelyLocked = false;
                        _parentLock.SendUpdateNotificationToAllThreadsAwaitingUpdateNotification();
                    }
                }
            }
        }
    }
}
