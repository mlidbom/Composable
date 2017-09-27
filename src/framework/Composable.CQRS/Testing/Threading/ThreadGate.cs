using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Transactions;
using Composable.Contracts;
using Composable.System.Threading.ResourceAccess;

namespace Composable.Testing.Threading
{
    class ThreadSnapshot
    {
        public Thread Thread { get; } = Thread.CurrentThread;


        public TransactionSnapshot Transaction { get; } = TransactionSnapshot.TakeSnapshot();
    }

    class TransactionSnapshot
    {
        public TransactionSnapshot(Transaction transaction)
        {
            IsolationLevel = transaction.IsolationLevel;
            TransactionInformation = new TransactionInformationSnapshot(transaction.TransactionInformation);

        }

        public class TransactionInformationSnapshot
        {
            public TransactionInformationSnapshot(TransactionInformation information)
            {
                LocalIdentifier = information.LocalIdentifier;
                DistributedIdentifier = information.DistributedIdentifier;
                Status = information.Status;
            }

            public string LocalIdentifier { get; }
            public Guid DistributedIdentifier { get; }
            public TransactionStatus Status { get; }
        }

        public IsolationLevel IsolationLevel { get; }

        public TransactionInformationSnapshot TransactionInformation { get; }

        public static TransactionSnapshot TakeSnapshot()
        {
            var currentTransaction = Transaction.Current;
            if(currentTransaction == null)
            {
                return null;
            }

            return  new TransactionSnapshot(currentTransaction);
        }
    }

    class ThreadGate : IThreadGate
    {
        public static IThreadGate CreateClosedWithTimeout(TimeSpan timeout) => new ThreadGate(timeout);
        public static IThreadGate CreateOpenWithTimeout(TimeSpan timeout) => new ThreadGate(timeout).Open();

        public TimeSpan DefaultTimeout => _defaultTimeout;
        public bool IsOpen => _isOpen;
        public long Queued => _resourceGuard.ExecuteWithResourceExclusivelyLocked(() => _queuedThreads.Count);
        public long Passed => _resourceGuard.ExecuteWithResourceExclusivelyLocked(() => _passedThreads.Count);
        public long Requested => _resourceGuard.ExecuteWithResourceExclusivelyLocked(() => _requestsThreads.Count);

        public IReadOnlyList<ThreadSnapshot> RequestedThreads => _resourceGuard.ExecuteWithResourceExclusivelyLocked(() => _requestsThreads.ToList());
        public IReadOnlyList<ThreadSnapshot> QueuedThreads => _resourceGuard.ExecuteWithResourceExclusivelyLocked(() => _queuedThreads.ToList());
        public IReadOnlyList<ThreadSnapshot> PassedThreads => _resourceGuard.ExecuteWithResourceExclusivelyLocked(() => _passedThreads.ToList());

        public IThreadGate Open()
        {
            using(var ownedLock = _resourceGuard.AwaitExclusiveLock())
            {
                _isOpen = true;
                _lockOnNextPass = false;
                ownedLock.SendUpdateNotificationToAllThreadsAwaitingUpdateNotification();
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
                ownedLock.SendUpdateNotificationToAllThreadsAwaitingUpdateNotification();
                return this.AwaitClosed();
            }
        }

        public bool TryAwait(TimeSpan timeout, Func<bool> condition) => _resourceGuard.TryAwait(timeout, condition);

        public IThreadGate ExecuteWithExclusiveLockWhen(TimeSpan timeout, Func<bool> condition, Action<IThreadGate, IExclusiveResourceLock> action)
        {
            using (var ownedLock = _resourceGuard.AwaitExclusiveLockWhen(timeout, condition))
            {
                action(this, ownedLock);
            }
            return this;
        }

        public IThreadGate Close()
        {
            _resourceGuard.ExecuteWithResourceExclusivelyLocked(() => _isOpen = false);
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
            _resourceGuard = ResourceAccessGuard.ExclusiveWithTimeout(defaultTimeout);
            _defaultTimeout = defaultTimeout;
        }

        readonly TimeSpan _defaultTimeout;
        readonly IExclusiveResourceAccessGuard _resourceGuard;
        bool _lockOnNextPass;
        bool _isOpen;
        readonly List<ThreadSnapshot> _requestsThreads = new List<ThreadSnapshot>();
        readonly LinkedList<ThreadSnapshot> _queuedThreads = new LinkedList<ThreadSnapshot>();
        readonly List<ThreadSnapshot> _passedThreads = new List<ThreadSnapshot>();
    }
}
