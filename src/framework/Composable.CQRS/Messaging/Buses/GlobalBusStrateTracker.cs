using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Composable.System;
using Composable.System.Collections.Collections;
using Composable.System.Linq;
using Composable.System.Threading.ResourceAccess;

namespace Composable.Messaging.Buses
{
    class GlobalBusStrateTracker : IGlobalBusStrateTracker
    {
        readonly List<QueuedMessage> _inflightMessages = new List<QueuedMessage>();

        //It is never OK for this class to block. So make that explicit with a really strict timeout on all operations waiting for access.
        readonly IExclusiveResourceAccessGuard _guard = ResourceAccessGuard.ExclusiveWithTimeout(1.Seconds());

        readonly Dictionary<IServiceBus, IList<Exception>> _busExceptions = new Dictionary<IServiceBus, IList<Exception>>();

        public IReadOnlyList<Exception> GetExceptionsFor(IServiceBus bus) => _guard.ExecuteWithResourceExclusivelyLocked(() => _busExceptions.GetOrAdd(bus, () => new List<Exception>()).ToList());
        public IExclusiveResourceAccessGuard ResourceGuard => _guard;

        public IGlobalBusStateSnapshot CreateSnapshotFor(IServiceBus bus)
            => _guard.ExecuteWithResourceExclusivelyLocked(
                () => new GlobalBusStateSnapshot(bus, _inflightMessages.ToList()));

        public IQueuedMessage AwaitDispatchableMessage(IServiceBus bus, IReadOnlyList<IMessageDispatchingRule> dispatchingRules)
        {
            IQueuedMessage result = null;
            using(var @lock = ResourceGuard.AwaitExclusiveLock())
            {
                do
                {
                    var snapshot = CreateSnapshotFor(bus);

                    result = snapshot
                        .LocallyQueuedMessages
                        .Where(queuedTask => !queuedTask.IsExecuting)
                        .FirstOrDefault(queuedTask => dispatchingRules.All(rule => rule.CanBeDispatched(snapshot, queuedTask)));

                    if (result == null)
                    {
                        @lock.ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock();
                    }
                }while(result == null);
                return result;

            }
        }


        public void EnqueueMessageTask(IServiceBus bus, IMessage message, Action messageTask)
            => _guard.ExecuteWithResourceExclusivelyLockedAndNotifyWaitingThreadsAboutUpdate(
                () =>
                {
                    var inflightMessage = new QueuedMessage(bus, message, this, messageTask);
                    _inflightMessages.Add(inflightMessage);
                    return inflightMessage;
                });

        public void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride)
            => _guard.ExecuteWithResourceExclusivelyLockedWhen(
                timeout: timeoutOverride ?? 30.Seconds(),
                condition: () => _inflightMessages.None(),
                action: () => {});

        void DoneWith(QueuedMessage queuedMessageInformation, Exception exception = null) => _guard.ExecuteWithResourceExclusivelyLockedAndNotifyWaitingThreadsAboutUpdate(() =>
        {
            if(exception != null)
            {
                _busExceptions.GetOrAdd(queuedMessageInformation.Bus, () => new List<Exception>()).Add(exception);
            }
            _inflightMessages.Remove(queuedMessageInformation);
        });

        class GlobalBusStateSnapshot : IGlobalBusStateSnapshot
        {
            public GlobalBusStateSnapshot(IServiceBus bus, IReadOnlyList<QueuedMessage> inflightMessages)
            {
                var bus1 = bus;
                InflightMessages = inflightMessages;
                LocallyExecutingMessages = inflightMessages.Where(message => message.Bus == bus1 && message.IsExecuting).ToList();
                LocallyQueuedMessages = inflightMessages.Where(message => message.Bus == bus1 && !message.IsExecuting).ToList();
            }

            public IReadOnlyList<IQueuedMessageInformation> InflightMessages { get; }
            public IReadOnlyList<IQueuedMessage> LocallyQueuedMessages { get; }
            public IReadOnlyList<IQueuedMessage> LocallyExecutingMessages { get; }
        }

        class QueuedMessage : IQueuedMessage
        {
            public readonly IServiceBus Bus;
            readonly GlobalBusStrateTracker _globalBusStrateTracker;

            public void Run()
            {
                _globalBusStrateTracker.ResourceGuard.ExecuteWithResourceExclusivelyLocked(() => IsExecuting = true);
                Task.Run(() =>
                {
                    try
                    {
                        MessageTask();
                        _globalBusStrateTracker.DoneWith(this);
                    }
                    catch(Exception exception)
                    {
                        _globalBusStrateTracker.DoneWith(this, exception);
                    }
                });
            }

            Action MessageTask { get; }
            public QueuedMessage(IServiceBus bus, IMessage message, GlobalBusStrateTracker globalBusStrateTracker, Action messageTask)
            {
                Bus = bus;
                _globalBusStrateTracker = globalBusStrateTracker;
                MessageTask = messageTask;
                Message = message;
            }
            public IMessage Message { get; }
            public bool IsExecuting { get; private set; }
        }
    }
}
