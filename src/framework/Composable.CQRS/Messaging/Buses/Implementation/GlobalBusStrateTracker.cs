using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.System;
using Composable.System.Collections.Collections;
using Composable.System.Linq;
using Composable.System.Threading.ResourceAccess;

namespace Composable.Messaging.Buses
{
    class GlobalBusStrateTracker : IGlobalBusStrateTracker
    {
        readonly List<QueuedMessage> _inflightMessages = new List<QueuedMessage>();

        //It is never OK for this class to block for a significant amount of time. So make that explicit with a really strict timeout on all operations waiting for access.
        readonly IGuardedResource _guard = GuardedResource.WithTimeout(10.Milliseconds());

        readonly Dictionary<IInterprocessTransport, IList<Exception>> _busExceptions = new Dictionary<IInterprocessTransport, IList<Exception>>();

        public IReadOnlyList<Exception> GetExceptionsFor(IInterprocessTransport bus) => _guard.Update(() => _busExceptions.GetOrAdd(bus, () => new List<Exception>()).ToList());

        public IQueuedMessage AwaitDispatchableMessage(IInterprocessTransport bus, IReadOnlyList<IMessageDispatchingRule> dispatchingRules)
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

        public void EnqueueMessageTask(IInterprocessTransport bus, IMessage message, Action messageTask)
            => _guard.Update(
                () =>
                {
                    var inflightMessage = new QueuedMessage(bus, message, this, messageTask);
                    _inflightMessages.Add(inflightMessage);
                    return inflightMessage;
                });

        public void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride)
            => _guard.AwaitCondition(timeout: timeoutOverride ?? 30.Seconds(),
                            condition: () => _inflightMessages.None());

        void Succeeded(QueuedMessage queuedMessageInformation)
            => _guard.Update(() => _inflightMessages.Remove(queuedMessageInformation));

        void Failed(QueuedMessage queuedMessageInformation, Exception exception)
            => _guard.Update(() =>
            {
                _busExceptions.GetOrAdd(queuedMessageInformation.Bus, () => new List<Exception>()).Add(exception);
                _inflightMessages.Remove(queuedMessageInformation);
            });

        class GlobalBusStateSnapshot : IGlobalBusStateSnapshot
        {
            public GlobalBusStateSnapshot(IInterprocessTransport bus, IReadOnlyList<QueuedMessage> inflightMessages)
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
            public readonly IInterprocessTransport Bus;
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

            public QueuedMessage(IInterprocessTransport bus, IMessage message, GlobalBusStrateTracker globalBusStrateTracker, Action messageTask)
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
