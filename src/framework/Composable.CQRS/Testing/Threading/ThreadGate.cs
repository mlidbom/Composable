using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.System.Threading.ResourceAccess;

namespace Composable.Testing.Threading
{
    class ThreadGate : IThreadGate
    {
        public static IThreadGate CreateClosedWithTimeout(TimeSpan timeout) => new ThreadGate(timeout);
        public static IThreadGate CreateOpenWithTimeout(TimeSpan timeout) => new ThreadGate(timeout).Open();

        public TimeSpan DefaultTimeout => _defaultTimeout;
        public bool IsOpen => _isOpen;
        public long Queued => _resourceGuard.Read(() => _queuedThreads.Count);
        public long Passed => _resourceGuard.Read(() => _passedThreads.Count);
        public long Requested => _resourceGuard.Read(() => _requestsThreads.Count);

        public IReadOnlyList<ThreadSnapshot> RequestedThreads => _resourceGuard.Read(() => _requestsThreads.ToList());
        public IReadOnlyList<ThreadSnapshot> QueuedThreads => _resourceGuard.Read(() => _queuedThreads.ToList());
        public IReadOnlyList<ThreadSnapshot> PassedThrough => _resourceGuard.Read(() => _passedThreads.ToList());
        public Action<ThreadSnapshot> PassThroughAction => _resourceGuard.Read(() => _passThroughAction);

        public IThreadGate Open()
        {
            using(var ownedLock = _resourceGuard.AwaitExclusiveLock())
            {
                _isOpen = true;
                _lockOnNextPass = false;
                ownedLock.NotifyWaitingThreadsAboutUpdate();
            }
            return this;
        }

        public IThreadGate AwaitLetOneThreadPassthrough()
        {
            using(var ownedLock = _resourceGuard.AwaitExclusiveLock())
            {
                Contract.Assert.That(!_isOpen, "Gate must be closed to call this method.");
                _isOpen = true;
                _lockOnNextPass = true;
                ownedLock.NotifyWaitingThreadsAboutUpdate();
                return this.AwaitClosed();
            }
        }

        public bool TryAwait(TimeSpan timeout, Func<bool> condition) => _resourceGuard.TryAwaitCondition(timeout, condition);

        public IThreadGate SetPassThroughAction(Action<ThreadSnapshot> action) => _resourceGuard.UpdateAndReturn(() => _passThroughAction = action, this);

        public IThreadGate ExecuteWithExclusiveLockWhen(TimeSpan timeout, Func<bool> condition, Action<IThreadGate, IExclusiveResourceLock> action)
        {
            using(var ownedLock = _resourceGuard.AwaitExclusiveLockWhen(timeout, condition))
            {
                action(this, ownedLock);
            }
            return this;
        }

        public IThreadGate Close()
        {
            _resourceGuard.Update(() => _isOpen = false);
            return this;
        }

        public void AwaitPassthrough() => AwaitPassthrough(_defaultTimeout);

        public void AwaitPassthrough(TimeSpan timeout)
        {
            using(var ownedLock = _resourceGuard.AwaitExclusiveLock(_defaultTimeout))
            {
                var currentThread = new ThreadSnapshot();
                _requestsThreads.Add(currentThread);
                _queuedThreads.AddLast(currentThread);
                ownedLock.NotifyWaitingThreadsAboutUpdate();
                while(!_isOpen)
                {
                    ownedLock.ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(_defaultTimeout);
                }

                if(_lockOnNextPass)
                {
                    _lockOnNextPass = false;
                    _isOpen = false;
                }

                _queuedThreads.Remove(currentThread);
                _passedThreads.Add(currentThread);
                ownedLock.NotifyWaitingThreadsAboutUpdate();
                _passThroughAction?.Invoke(currentThread);
            }
        }

        ThreadGate(TimeSpan defaultTimeout)
        {
            _resourceGuard = GuardedResource.WithTimeout(defaultTimeout);
            _defaultTimeout = defaultTimeout;
        }

        readonly TimeSpan _defaultTimeout;
        readonly IGuardedResource _resourceGuard;
        bool _lockOnNextPass;
        Action<ThreadSnapshot> _passThroughAction;
        bool _isOpen;
        readonly List<ThreadSnapshot> _requestsThreads = new List<ThreadSnapshot>();
        readonly LinkedList<ThreadSnapshot> _queuedThreads = new LinkedList<ThreadSnapshot>();
        readonly List<ThreadSnapshot> _passedThreads = new List<ThreadSnapshot>();
    }
}
