using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Composable.Contracts;
using Composable.System.Threading.ResourceAccess;

namespace Composable.Testing.Threading
{
    class ThreadGate : IThreadGate
    {
        public static IThreadGate WithTimeout(TimeSpan timeout) => new ThreadGate(timeout);

        public TimeSpan DefaultTimeout => _defaultTimeout;
        public bool IsOpen => _isOpen;
        public long Queued => _lock.ExecuteWithExclusiveLock(() => _queuedThreads.Count);
        public long Passed => _lock.ExecuteWithExclusiveLock(() => _passedThreads.Count);
        public long Requested => _lock.ExecuteWithExclusiveLock(() => _requestsThreads.Count);

        public IReadOnlyList<Thread> RequestedThreads => _lock.ExecuteWithExclusiveLock(() => _requestsThreads.ToList());
        public IReadOnlyList<Thread> QueuedThreads => _lock.ExecuteWithExclusiveLock(() => _queuedThreads.ToList());
        public IReadOnlyList<Thread> PassedThreads => _lock.ExecuteWithExclusiveLock(() => _passedThreads.ToList());

        public IThreadGate Open()
        {
            using(var ownedLock = _lock.AwaitExclusiveLock())
            {
                Contract.Assert.That(!_isOpen, "Gate must be closed to call this method.");
                _isOpen = true;
                _lockOnNextPass = false;
                ownedLock.SendUpdateNotificationToAllThreadsAwaitingUpdateNotification();
            }
            return this;
        }

        public IThreadGate LetOneThreadPass()
        {
            using(var ownedLock = _lock.AwaitExclusiveLock())
            {
                Contract.Assert.That(!_isOpen, "Gate must be closed to call this method.");
                _isOpen = true;
                _lockOnNextPass = true;
                ownedLock.SendUpdateNotificationToAllThreadsAwaitingUpdateNotification();
                return this.AwaitClosed();
            }
        }

        public IThreadGate ExecuteLockedOnce(TimeSpan timeout, Func<bool> condition, Action<IThreadGate, IExclusiveResourceLock> action)
        {
            using (var ownedLock = _lock.AwaitExclusiveLockWhen(timeout, condition))
            {
                action(this, ownedLock);
            }
            return this;
        }

        public IThreadGate Close()
        {
            _lock.ExecuteWithExclusiveLock(() => _isOpen = false);
            return this;
        }

        public void Pass() => Pass(_defaultTimeout);

        public void Pass(TimeSpan timeout)
        {
            using(var ownedLock = _lock.AwaitExclusiveLock(_defaultTimeout))
            {
                var currentThread = Thread.CurrentThread;
                _requestsThreads.Add(currentThread);
                _queuedThreads.AddLast(currentThread);
                ownedLock.SendUpdateNotificationToAllThreadsAwaitingUpdateNotification();
                while(!_isOpen)
                {
                    ownedLock.ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock();
                }

                if(_lockOnNextPass)
                {
                    _lockOnNextPass = false;
                    _isOpen = false;
                }

                _queuedThreads.Remove(currentThread);
                _passedThreads.Add(currentThread);
                ownedLock.SendUpdateNotificationToAllThreadsAwaitingUpdateNotification();
            }
        }

        ThreadGate(TimeSpan defaultTimeout)
        {
            _lock = ResourceAccessGuard.ExclusiveWithTimeout(defaultTimeout);
            _defaultTimeout = defaultTimeout;
        }

        readonly TimeSpan _defaultTimeout;
        readonly IExclusiveResourceAccessGuard _lock;
        bool _lockOnNextPass;
        bool _isOpen;
        readonly List<Thread> _requestsThreads = new List<Thread>();
        readonly LinkedList<Thread> _queuedThreads = new LinkedList<Thread>();
        readonly List<Thread> _passedThreads = new List<Thread>();
    }
}
