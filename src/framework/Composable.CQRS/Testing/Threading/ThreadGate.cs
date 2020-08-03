using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;

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
                ownedLock.NotifyAllWaitingThreads();
            }
            return this;
        }

        public IThreadGate AwaitLetOneThreadPassThrough()
        {
            using var ownedLock = _resourceGuard.AwaitExclusiveLock();
            Contract.Assert.That(!_isOpen, "Gate must be closed to call this method.");
            _isOpen = true;
            _lockOnNextPass = true;
            ownedLock.NotifyAllWaitingThreads();
            return this.AwaitClosed();
        }

        public bool TryAwait(TimeSpan timeout, Func<bool> condition) => _resourceGuard.TryAwaitCondition(timeout, condition);

        public IThreadGate SetPostPassThroughAction(Action<ThreadSnapshot> action) => this.Mutate(_ => _resourceGuard.Update(() => _postPassThroughAction = action));
        public IThreadGate SetPrePassThroughAction(Action<ThreadSnapshot> action) => this.Mutate(_ => _resourceGuard.Update(() => _prePassThroughAction = action));
        public IThreadGate SetPassThroughAction(Action<ThreadSnapshot> action) => this.Mutate(_ => _resourceGuard.Update(() => _passThroughAction = action));

        public IThreadGate ExecuteWithExclusiveLockWhen(TimeSpan timeout, Func<bool> condition, Action action)
        {
            try
            {
                using(_resourceGuard.AwaitUpdateLockWhen(timeout, condition))
                {
                    action();
                }
            }
            catch(AwaitingConditionTimedOutException parentException)
            {
                throw new AwaitingConditionTimedOutException(parentException, $@"
Current state of gate: 
{this}");
            }
            return this;
        }

        public IThreadGate Close()
        {
            _resourceGuard.Update(() => _isOpen = false);
            return this;
        }

        public void AwaitPassThrough() => AwaitPassThrough(_defaultTimeout);

        public void AwaitPassThrough(TimeSpan timeout)
        {
            var currentThread = new ThreadSnapshot();

            _resourceGuard.Update(() =>
            {
                _requestsThreads.Add(currentThread);
                _queuedThreads.AddLast(currentThread);
            });


            using(_resourceGuard.AwaitUpdateLockWhen(() => _isOpen))
            {
                if(_lockOnNextPass)
                {
                    _lockOnNextPass = false;
                    _isOpen = false;
                }

                _queuedThreads.Remove(currentThread);
                _passedThreads.Add(currentThread);
                _prePassThroughAction.Invoke(currentThread);
                _passThroughAction.Invoke(currentThread);
                _postPassThroughAction.Invoke(currentThread);
            }
        }

        ThreadGate(TimeSpan defaultTimeout)
        {
            _resourceGuard = ResourceGuard.WithTimeout(defaultTimeout);
            _defaultTimeout = defaultTimeout;
        }

        public override string ToString() =>  $@"{nameof(IsOpen)} : {IsOpen},
{nameof(Queued)}: {Queued},
{nameof(Passed)}: {Passed},
{nameof(Requested)}: {Requested},
";

        readonly TimeSpan _defaultTimeout;
        readonly IResourceGuard _resourceGuard;
        bool _lockOnNextPass;
        Action<ThreadSnapshot> _passThroughAction = _ => { };
        Action<ThreadSnapshot> _prePassThroughAction = _ => { };
        Action<ThreadSnapshot> _postPassThroughAction = _ => { };
        bool _isOpen;
        readonly List<ThreadSnapshot> _requestsThreads = new List<ThreadSnapshot>();
        readonly LinkedList<ThreadSnapshot> _queuedThreads = new LinkedList<ThreadSnapshot>();
        readonly List<ThreadSnapshot> _passedThreads = new List<ThreadSnapshot>();
    }
}
