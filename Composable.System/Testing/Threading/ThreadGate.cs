using System;
using Composable.Contracts;
using Composable.System.Threading;

namespace Composable.Testing.Threading
{
    interface IThreadGateVisitor
    {
        void PassThrough();
        void PassThrough(TimeSpan timeout);
    }

    interface IThreadGate : IThreadGateVisitor
    {
        ///<summary>Opens the gate and lets all threads through.</summary>
        IThreadGate Open();

        ///<summary>Lets a single thread through.</summary>
        IThreadGate LetOneThreadPassThrough();

        ///<summary>Blocks all threads from passing.</summary>
        IThreadGate Close();

        ///<summary>Blocks until the gate is in a state which satisfies <see cref="condition"/> and then while owning the lock execuces <see cref="action"/></summary>
        IThreadGate ExecuteOnce(TimeSpan timeout, Predicate<IThreadGate> condition, Action<IThreadGate, IObjectLockOwner> action);

        bool IsOpen { get; }
        long QueueLength { get; }
        long RequestCount { get; }
        long PassedThrough { get; }
        TimeSpan DefaultTimeout { get; }
    }

    static class ThreadGateExtensions
    {
        public static IThreadGate WaitUntilClosed(this IThreadGate @this) => @this.WaitUntil(_ => !@this.IsOpen);
        public static IThreadGate WaitUntil(this IThreadGate @this, Predicate<IThreadGate> condition) => @this.WaitUntil(@this.DefaultTimeout, condition);
        public static IThreadGate WaitUntil(this IThreadGate @this, TimeSpan timeout, Predicate<IThreadGate> condition) => @this.ExecuteOnce(@this.DefaultTimeout, condition, (gate, owner) => { });
        public static IThreadGate WaitUntilQueueIsEmpty(this IThreadGate @this) => @this.WaitUntil(me => me.QueueLength == 0);
    }

    class ThreadGate : IThreadGate
    {
        public static IThreadGate WithTimeout(TimeSpan timeout) => new ThreadGate("unnamed", timeout);

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

        public IThreadGate LetOneThreadPassThrough()
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

        public TimeSpan DefaultTimeout => _defaultTimeout;
        public long QueueLength => _settingsLock.ExecuteWithExclusiveLock(() => _requestedPassThrough - _passedThrough);
        public long PassedThrough => _settingsLock.ExecuteWithExclusiveLock(() => _passedThrough);
        public long RequestCount => _settingsLock.ExecuteWithExclusiveLock(() => _requestedPassThrough);
        public bool IsOpen => _open;

        public IThreadGate Close()
        {
            _settingsLock.ExecuteWithExclusiveLock(() => _open = false);
            return this;
        }

        public void PassThrough() => PassThrough(_defaultTimeout);

        public void PassThrough(TimeSpan timeout)
        {
            using(var ownedLock = _settingsLock.LockForExclusiveUse(_defaultTimeout))
            {
                _requestedPassThrough++;
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
                _passedThrough++;
                ownedLock.PulseAll();
            }
        }

        public string Name { get; }
        bool _lockOnNextPass;
        long _requestedPassThrough;
        long _passedThrough;

        ThreadGate(string name, TimeSpan defaultTimeout)
        {
            _settingsLock = ObjectLock.WithTimeout(defaultTimeout);
            _defaultTimeout = defaultTimeout;
            Name = name;
        }

        public readonly TimeSpan _defaultTimeout;
        readonly IObjectLock _settingsLock;
        bool _open;
    }
}
