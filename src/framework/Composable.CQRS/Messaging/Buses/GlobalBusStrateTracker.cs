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
        readonly IExclusiveResourceAccessGuard _guard = ResourceAccessGuard.ExclusiveWithTimeout(1.Milliseconds());

        readonly Dictionary<IServiceBus, IList<Exception>> _busExceptions = new Dictionary<IServiceBus, IList<Exception>>();

        public IReadOnlyList<Exception> GetExceptionsFor(IServiceBus bus) => _guard.ExecuteWithResourceExclusivelyLocked(() => _busExceptions.GetOrAdd(bus, () => new List<Exception>()).ToList());

        public IQueuedMessage AwaitDispatchableMessage(IServiceBus bus, IReadOnlyList<IMessageDispatchingRule> dispatchingRules)
        {
            using(var @lock = _guard.AwaitExclusiveLock())
            {
                QueuedMessage result;
                do
                {
                    var snapshot = new GlobalBusStateSnapshot(bus, _inflightMessages.ToList());

                    result = _inflightMessages
                        .Where(queuedMessage => queuedMessage.Bus == bus && !queuedMessage.IsExecuting)
                        .FirstOrDefault(queuedTask => dispatchingRules.All(rule => rule.CanBeDispatched(snapshot, queuedTask)));

                    if(result == null)
                    {
                        @lock.ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(7.Days());
                    }
                } while(result == null);
                result.SetIsExecuting();
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
            => _guard.Await(timeout: timeoutOverride ?? 30.Seconds(),
                            condition: () => _inflightMessages.None());

        void Succeeded(QueuedMessage queuedMessageInformation)
            => _guard.ExecuteWithResourceExclusivelyLockedAndNotifyWaitingThreadsAboutUpdate(() => _inflightMessages.Remove(queuedMessageInformation));

        void Failed(QueuedMessage queuedMessageInformation, Exception exception)
            => _guard.ExecuteWithResourceExclusivelyLockedAndNotifyWaitingThreadsAboutUpdate(() =>
            {
                _busExceptions.GetOrAdd(queuedMessageInformation.Bus, () => new List<Exception>()).Add(exception);
                _inflightMessages.Remove(queuedMessageInformation);
            });

        class GlobalBusStateSnapshot : IGlobalBusStateSnapshot
        {
            public GlobalBusStateSnapshot(IServiceBus bus, IReadOnlyList<QueuedMessage> inflightMessages)
            {
                var bus1 = bus;
                InflightMessages = inflightMessages;
                LocallyExecutingMessages = inflightMessages.Where(message => message.Bus == bus1 && message.IsExecuting).ToList();
            }

            public IReadOnlyList<IQueuedMessageInformation> InflightMessages { get; }
            public IReadOnlyList<IQueuedMessage> LocallyExecutingMessages { get; }
        }

        class QueuedMessage : IQueuedMessage
        {
            public readonly IServiceBus Bus;
            readonly GlobalBusStrateTracker _globalBusStrateTracker;
            readonly Action _messageTask;
            public IMessage Message { get; }
            public bool IsExecuting { get; private set; }

            public void Run()
            {
                Task.Run(() =>
                {
                    try
                    {
                        _messageTask();
                        _globalBusStrateTracker.Succeeded(this);
                    }
                    catch(Exception exception)
                    {
                        _globalBusStrateTracker.Failed(this, exception);
                    }
                });
            }

            public QueuedMessage(IServiceBus bus, IMessage message, GlobalBusStrateTracker globalBusStrateTracker, Action messageTask)
            {
                Bus = bus;
                _globalBusStrateTracker = globalBusStrateTracker;
                _messageTask = messageTask;
                Message = message;
            }

            public void SetIsExecuting() => IsExecuting = true;
        }
    }
}
