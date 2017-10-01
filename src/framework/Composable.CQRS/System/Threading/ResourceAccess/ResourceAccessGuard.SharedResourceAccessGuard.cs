using System;
using System.Linq;
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
            int _threadsWithSharedLocks;

            readonly ThreadLocal<int> _currentThreadUnreleasedExclusiveLocks = new ThreadLocal<int>(trackAllValues:true, valueFactory: () => 0);
            readonly ThreadLocal<int> _currentThreadUnreleasedSharedLocks = new ThreadLocal<int>(trackAllValues: true, valueFactory: () => 0);

            readonly ExclusiveResourceAccessGuard _resourceGuard;

            public SharedResourceAccessGuard(int maxSharedLocks, TimeSpan defaultTimeout)
            {
                _resourceGuard = new ExclusiveResourceAccessGuard(defaultTimeout);
                _maxSharedLocks = maxSharedLocks;
                AssertInvariantsAreMet();
            }

            public IDisposable AwaitSharedLock(TimeSpan? timeoutOverride = null)
            {
                AssertInvariantsAreMet();
                using (var exclusiveLock = _resourceGuard.AwaitExclusiveLock(timeoutOverride))
                {
                    while (!CurrentThreadCanAcquireReadLock)
                    {
                        exclusiveLock.ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(timeoutOverride ?? _resourceGuard._defaultTimeout);
                    }

                    _currentThreadUnreleasedSharedLocks.Value++;
                    if(_currentThreadUnreleasedSharedLocks.Value == 1)
                    {
                        _threadsWithSharedLocks++;
                    }

                    AssertInvariantsAreMet();

                    return Disposable.Create(
                        () =>
                        {
                            using (var disposingExclusiveLock = _resourceGuard.AwaitExclusiveLock())
                            {
                                _currentThreadUnreleasedSharedLocks.Value--;
                                if(_currentThreadUnreleasedSharedLocks.Value == 0)
                                {
                                    _threadsWithSharedLocks--;
                                }
                                disposingExclusiveLock.SendUpdateNotificationToOneThreadAwaitingUpdateNotification();
                                AssertInvariantsAreMet();
                            }
                        });
                }
            }


            bool CurrentThreadCanAcquireReadLock => CurrentThreadHasSharedLock || SharedLocksAreAvailable;
            bool CurrentThreadHasSharedLock => _currentThreadUnreleasedSharedLocks.Value != 0;
            bool SharedLocksAreAvailable => !_isExclusivelyLocked && _threadsWaitingForExclusiveLock == 0 && !IsAtSharedLockLimit;
            bool IsAtSharedLockLimit => _threadsWithSharedLocks == _maxSharedLocks;

            public IExclusiveResourceLock AwaitExclusiveLock(TimeSpan? timeoutOverride = null)
            {
                AssertInvariantsAreMet();
                IExclusiveResourceLock exclusiveLock = null;
                try
                {
                    exclusiveLock = _resourceGuard.AwaitExclusiveLock(timeoutOverride);
                    AwaitExclusiveLock(timeoutOverride, exclusiveLock);
                }
                catch (Exception)
                {
                    exclusiveLock?.Dispose();
                    throw;
                }

                AssertInvariantsAreMet();
                return new ExclusiveResourceAccessLockToSharedResource(this, exclusiveLock);
            }

            void AwaitExclusiveLock(TimeSpan? timeoutOverride, IExclusiveResourceLock exclusiveLock)
            {
                _threadsWaitingForExclusiveLock++;
                while (!CurrentThreadCanAquireExclusiveLock)
                {
                    exclusiveLock.ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(timeoutOverride ?? _resourceGuard._defaultTimeout);
                }

                _currentThreadUnreleasedExclusiveLocks.Value++;
                _isExclusivelyLocked = true;
                _threadsWaitingForExclusiveLock--;
            }

            bool CurrentThreadCanAquireExclusiveLock => CurrentThreadHoldsExclusiveLock || ExclusiveLockIsAvailable;
            bool ExclusiveLockIsAvailable => !_isExclusivelyLocked && OnlyCurrentThreadsHoldsAReadLock;
            bool OnlyCurrentThreadsHoldsAReadLock => _threadsWithSharedLocks - _currentThreadUnreleasedSharedLocks.Value == 0;
            bool CurrentThreadHoldsExclusiveLock => _currentThreadUnreleasedExclusiveLocks.Value != 0;

            void AssertInvariantsAreMet()
            {
                var currentThreadUnreleasedSharedLocks = _currentThreadUnreleasedSharedLocks.Value;
                var currentThreadUnreleasedExclusiveLocks = _currentThreadUnreleasedExclusiveLocks.Value;

                Assert(currentThreadUnreleasedSharedLocks > -1, "Current thread cannot have a negative number of shared locks.");
                Assert(currentThreadUnreleasedExclusiveLocks > -1, "Current thread cannot have a negative number of exclusive locks.");
                Assert(_threadsWithSharedLocks <= _maxSharedLocks, "Shared locks must not exceed maximum limit.");
                Assert(currentThreadUnreleasedSharedLocks == 0 || _threadsWithSharedLocks > 0, "If current thread has shared lock there must be shared locks");
                Assert(currentThreadUnreleasedExclusiveLocks == 0 || _isExclusivelyLocked, "If current thread has exclusive lock there must be an exclusive lock.");
                Assert(currentThreadUnreleasedExclusiveLocks == 0 || _threadsWithSharedLocks - currentThreadUnreleasedSharedLocks == 0, "If current thread has exclusive lock no other threads can have shared locks");
                Assert(!_isExclusivelyLocked || _currentThreadUnreleasedExclusiveLocks.Values.Count( unreleased => unreleased > 0) == 1, "If there is an exclusive lock exactly one thread has an exclusive lock");
                Assert(_threadsWithSharedLocks  == 0 || _currentThreadUnreleasedSharedLocks.Values.Count(unreleased => unreleased > 0) == 1, "If there are shared locks the threads with shared locks match the recorded number");
            }

            // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
            static void Assert(bool condition, string error)
            {
                if(!condition)
                {
                    throw new Exception(error);
                }
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

                public bool TryReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(TimeSpan timeout) => _parentLock.TryReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(timeout);

                public void ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(TimeSpan timeout)
                {
                    _parent.AssertInvariantsAreMet();
                    _parent._isExclusivelyLocked = false;
                    _parent.AssertInvariantsAreMet();
                    _parent.AwaitExclusiveLock(timeout, _parentLock);
                    _parent.AssertInvariantsAreMet();
                }

                public void Dispose()
                {
                    try
                    {
                        //todo: Log a warning if disposing after a longer time than the default lock timeout.
                        _parent._currentThreadUnreleasedExclusiveLocks.Value--;
                        if(_parent._currentThreadUnreleasedExclusiveLocks.Value == 0)
                        {
                            _parent._isExclusivelyLocked = false;
                            _parentLock.SendUpdateNotificationToOneThreadAwaitingUpdateNotification();
                        }
                        _parent.AssertInvariantsAreMet();
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
