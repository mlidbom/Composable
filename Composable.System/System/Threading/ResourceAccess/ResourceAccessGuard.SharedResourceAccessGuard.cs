using System;
using System.Threading;

namespace Composable.System.Threading.ResourceAccess
{
    static partial class ResourceAccessGuard
    {
        class SharedResourceAccessGuard : ISharedResourceAccessGuard
        {
            readonly int _maxSharedLocks;
            int _threadsWaitingForExclusiveLock;
            bool _isExclusivelyLocked;

            readonly ThreadLocal<int> _exclusiveLockRecursionLevel = new ThreadLocal<int>();
            readonly ThreadLocal<int> _sharedLockRecursionLevel = new ThreadLocal<int>();

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
                    if (!_sharedLockRecursionLevel.IsValueCreated)
                    {
                        _sharedLockRecursionLevel.Value = 0;
                    }

                    while (!CurrentThreadCanAcquireReadLock)
                    {
                        exclusiveLock.ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(timeoutOverride);
                    }

                    _sharedLockRecursionLevel.Value++;
                    if(_sharedLockRecursionLevel.Value == 1)
                    {
                        _currentSharedLocks++;
                    }

                    return Disposable.Create(
                        () =>
                        {
                            using (var disposingExclusiveLock = _exclusiveAccessGuard.AwaitExclusiveLock())
                            {
                                _sharedLockRecursionLevel.Value--;
                                if(_sharedLockRecursionLevel.Value == 0)
                                {
                                    _currentSharedLocks--;
                                }
                                disposingExclusiveLock.SendUpdateNotificationToOneThreadAwaitingUpdateNotification();
                            }
                        });
                }
            }

            bool CurrentThreadCanAcquireReadLock => _sharedLockRecursionLevel.Value != 0 || (!_isExclusivelyLocked && _threadsWaitingForExclusiveLock == 0 && !AtSharedLockLimit);
            bool AtSharedLockLimit => _currentSharedLocks == _maxSharedLocks;

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
                if(!_exclusiveLockRecursionLevel.IsValueCreated)
                {
                    _exclusiveLockRecursionLevel.Value = 0;
                }

                _threadsWaitingForExclusiveLock++;
                while (!CurrentThreadCanAquireExclusiveLock)
                {
                    exclusiveLock.ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(timeoutOverride);
                }

                _exclusiveLockRecursionLevel.Value++;
                _isExclusivelyLocked = true;
                _threadsWaitingForExclusiveLock--;
            }

            bool CurrentThreadCanAquireExclusiveLock => CurrentThreadHoldsExclusiveLock || (!_isExclusivelyLocked && OnlyCurrentThreadsHoldsAReadLock);
            bool OnlyCurrentThreadsHoldsAReadLock => _currentSharedLocks - _sharedLockRecursionLevel.Value == 0;
            bool CurrentThreadHoldsExclusiveLock => _exclusiveLockRecursionLevel.Value != 0;

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
                    _parent._isExclusivelyLocked = false;
                    _parent.AwaitExclusiveLock(timeoutOverride);
                }

                public void Dispose()
                {
                    try
                    {
                        _parent._exclusiveLockRecursionLevel.Value--;
                        if(_parent._exclusiveLockRecursionLevel.Value == 0)
                        {
                            _parent._isExclusivelyLocked = false;
                        }
                        _parentLock.SendUpdateNotificationToAllThreadsAwaitingUpdateNotification();
                    }
                    finally
                    {
                        _parentLock.Dispose();
                    }
                }
            }
        }
    }
}
