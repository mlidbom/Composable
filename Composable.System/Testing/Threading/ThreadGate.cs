using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Composable.Contracts;
using Composable.System.Threading;
using Composable.System.Threading.ResourceAccess;

namespace Composable.Testing.Threading
{
    interface IThreadGateVisitor
    {
        void Pass();
        void Pass(TimeSpan timeout);
    }

    interface IThreadGate : IThreadGateVisitor
    {
        ///<summary>Opens the gate and lets all threads through.</summary>
        IThreadGate Open();

        ///<summary>Lets a single thread pass.</summary>
        IThreadGate LetOneThreadPass();

        ///<summary>Blocks all threads from passing.</summary>
        IThreadGate Close();

        ///<summary>Blocks until the gate is in a state which satisfies <see cref="condition"/> and then while owning the lock executes <see cref="action"/></summary>
        IThreadGate ExecuteLockedOnce(TimeSpan timeout, Predicate<IThreadGate> condition, Action<IThreadGate, IExclusiveResourceLock> action);

        bool IsOpen { get; }
        long Queued { get; }
        long Requested { get; }
        long Passed { get; }
        TimeSpan DefaultTimeout { get; }

        IReadOnlyList<Thread> RequestedThreads { get; }
        IReadOnlyList<Thread> QueuedThreads { get; }
        IReadOnlyList<Thread> PassedThreads { get; }
    }

    static class ThreadGateExtensions
    {
        public static IThreadGate Await(this IThreadGate @this, Predicate<IThreadGate> condition) => @this.Await(@this.DefaultTimeout, condition);
        public static IThreadGate Await(this IThreadGate @this, TimeSpan timeout, Predicate<IThreadGate> condition) => @this.ExecuteLockedOnce(timeout, condition, (gate, owner) => {});
        public static IThreadGate AwaitClosed(this IThreadGate @this) => @this.Await(_ => !@this.IsOpen);
        public static IThreadGate AwaitQueueLength(this IThreadGate @this, int length) => @this.Await(me => me.Queued == length);
        public static IThreadGate AwaitEmptyQueue(this IThreadGate @this) => @this.Await(me => me.Queued == 0);
        public static IThreadGate WithExclusiveLock(this IThreadGate @this, Action action) => @this.ExecuteLockedOnce(@this.DefaultTimeout, _ => true, (gate,owner)=> action());
    }

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
            }
            return this.AwaitClosed();
        }

        public IThreadGate ExecuteLockedOnce(TimeSpan timeout, Predicate<IThreadGate> condition, Action<IThreadGate, IExclusiveResourceLock> action)
        {
            using(var ownedLock = _lock.AwaitExclusiveLock(timeout))
            {
                while(!condition(this))
                {
                    ownedLock.ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock();
                }
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
            _lock = ResourceAccessGuard.WithTimeout(defaultTimeout);
            _defaultTimeout = defaultTimeout;
        }

        readonly TimeSpan _defaultTimeout;
        readonly IExclusiveResourceLockManager _lock;
        bool _lockOnNextPass;
        bool _isOpen;
        readonly List<Thread> _requestsThreads = new List<Thread>();
        readonly LinkedList<Thread> _queuedThreads = new LinkedList<Thread>();
        readonly List<Thread> _passedThreads = new List<Thread>();
    }
}
