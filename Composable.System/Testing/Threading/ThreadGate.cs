using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Composable.Contracts;
using Composable.System.Threading;

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

        ///<summary>Blocks until the gate is in a state which satisfies <see cref="condition"/> and then while owning the lock execuces <see cref="action"/></summary>
        IThreadGate ExecuteOnce(TimeSpan timeout, Predicate<IThreadGate> condition, Action<IThreadGate, IObjectLockOwner> action);

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
        public static IThreadGate Await(this IThreadGate @this, TimeSpan timeout, Predicate<IThreadGate> condition) => @this.ExecuteOnce(timeout, condition, (gate, owner) => {});
        public static IThreadGate AwaitClosed(this IThreadGate @this) => @this.Await(_ => !@this.IsOpen);
        public static IThreadGate AwaitEmptyQueue(this IThreadGate @this) => @this.Await(me => me.Queued == 0);
    }

    class ThreadGate : IThreadGate
    {
        public static IThreadGate WithTimeout(TimeSpan timeout) => new ThreadGate("unnamed", timeout);

        public TimeSpan DefaultTimeout => _defaultTimeout;
        public bool IsOpen => _open;
        public long Queued => _settingsLock.ExecuteWithExclusiveLock(() => _queuedThreads.Count);
        public long Passed => _settingsLock.ExecuteWithExclusiveLock(() => _passedThreads.Count);
        public long Requested => _settingsLock.ExecuteWithExclusiveLock(() => _requestsThreads.Count);

        public IReadOnlyList<Thread> RequestedThreads => _settingsLock.ExecuteWithExclusiveLock(() => _requestsThreads.ToList());
        public IReadOnlyList<Thread> QueuedThreads => _settingsLock.ExecuteWithExclusiveLock(() => _queuedThreads.ToList());
        public IReadOnlyList<Thread> PassedThreads => _settingsLock.ExecuteWithExclusiveLock(() => _passedThreads.ToList());

        public IThreadGate Open()
        {
            using(var ownedLock = _settingsLock.LockForExclusiveUse())
            {
                Contract.Assert.That(!_open, "Gate must be closed to call this method.");
                _open = true;
                _lockOnNextPass = false;
                ownedLock.PulseAll();
            }
            return this;
        }

        public IThreadGate LetOneThreadPass()
        {
            using(var ownedLock = _settingsLock.LockForExclusiveUse())
            {
                Contract.Assert.That(!_open, "Gate must be closed to call this method.");
                _open = true;
                _lockOnNextPass = true;
                ownedLock.PulseAll();
            }
            return this;
        }

        public IThreadGate ExecuteOnce(TimeSpan timeout, Predicate<IThreadGate> condition, Action<IThreadGate, IObjectLockOwner> action)
        {
            using(var ownedLock = _settingsLock.LockForExclusiveUse(timeout))
            {
                while(!condition(this))
                {
                    ownedLock.Wait();
                }
                action(this, ownedLock);
            }
            return this;
        }

        public IThreadGate Close()
        {
            _settingsLock.ExecuteWithExclusiveLock(() => _open = false);
            return this;
        }

        public void Pass() => Pass(_defaultTimeout);

        public void Pass(TimeSpan timeout)
        {
            using(var ownedLock = _settingsLock.LockForExclusiveUse(_defaultTimeout))
            {
                var currentThread = Thread.CurrentThread;
                _requestsThreads.Add(currentThread);
                _queuedThreads.AddLast(currentThread);
                ownedLock.PulseAll();
                while(!_open)
                {
                    ownedLock.Wait();
                }

                if(_lockOnNextPass)
                {
                    _lockOnNextPass = false;
                    _open = false;
                }

                _queuedThreads.Remove(currentThread);
                _passedThreads.Add(currentThread);
                ownedLock.PulseAll();
            }
        }

        public string Name { get; }
        bool _lockOnNextPass;

        ThreadGate(string name, TimeSpan defaultTimeout)
        {
            _settingsLock = ObjectLock.WithTimeout(defaultTimeout);
            _defaultTimeout = defaultTimeout;
            Name = name;
        }

        public readonly TimeSpan _defaultTimeout;
        readonly IObjectLock _settingsLock;
        bool _open;

        List<Thread> _requestsThreads = new List<Thread>();
        LinkedList<Thread> _queuedThreads = new LinkedList<Thread>();
        List<Thread> _passedThreads = new List<Thread>();
    }
}
