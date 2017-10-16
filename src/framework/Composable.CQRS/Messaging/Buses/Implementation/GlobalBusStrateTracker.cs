using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Composable.System;
using Composable.System.Collections.Collections;
using Composable.System.Linq;
using Composable.System.Threading.ResourceAccess;

namespace Composable.Messaging.Buses.Implementation
{
    class GlobalBusStrateTracker : IGlobalBusStrateTracker
    {
        readonly List<QueuedMessage> _inflightMessages = new List<QueuedMessage>();

        //Todo: It is never OK for this class to block for a significant amount of time. So make that explicit with a really strict timeout on all operations waiting for access.
        //Currently we cannot make the timeout really strict because it does time out....
        readonly IResourceGuard _guard = ResourceGuard.WithTimeout(100.Milliseconds());

        readonly Dictionary<IInbox, IList<Exception>> _busExceptions = new Dictionary<IInbox, IList<Exception>>();
        readonly Dictionary<Guid, int> _inflightMessageIds = new Dictionary<Guid, int>();

        public IReadOnlyList<Exception> GetExceptionsFor(IInbox bus) => _guard.Update(() => _busExceptions.GetOrAdd(bus, () => new List<Exception>()).ToList());

        public IQueuedMessage AwaitDispatchableMessage(IInbox bus, IReadOnlyList<IMessageDispatchingRule> dispatchingRules)
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

        public void SendingMessageOnTransport(TransportMessage.OutGoing message) => _guard.Update(() =>
        {
            var value = _inflightMessageIds.GetOrAdd(message.MessageId, () => 0);
            _inflightMessageIds[message.MessageId] = value + 1;
        });

        public void EnqueueMessageTask(IInbox bus, TransportMessage.InComing message, Action messageTask) => _guard.Update(() =>
        {
            var inflightMessage = new QueuedMessage(bus, message, this, messageTask);
            _inflightMessages.Add(inflightMessage);
            return inflightMessage;
        });

        public void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride)
            => _guard.AwaitCondition(timeout: timeoutOverride ?? 30.Seconds(),
                                     condition: () => _inflightMessages.None() && _inflightMessageIds.None());

        void Succeeded(QueuedMessage queuedMessageInformation) => _guard.Update(() => DoneDispatching(queuedMessageInformation));

        void Failed(QueuedMessage queuedMessageInformation, Exception exception) => _guard.Update(() =>
        {
            _busExceptions.GetOrAdd(queuedMessageInformation.Bus, () => new List<Exception>()).Add(exception);
            DoneDispatching(queuedMessageInformation);
        });

        void DoneDispatching(QueuedMessage queuedMessageInformation)
        {
            _inflightMessages.Remove(queuedMessageInformation);
            var currentCount = _inflightMessageIds[queuedMessageInformation.MessageId] -= 1;
            if(currentCount == 0)
            {
                _inflightMessageIds.Remove(queuedMessageInformation.MessageId);
            }
        }

        class GlobalBusStateSnapshot : IGlobalBusStateSnapshot
        {
            public GlobalBusStateSnapshot(IInbox bus, IReadOnlyList<QueuedMessage> inflightMessages)
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
            public readonly IInbox Bus;
            readonly GlobalBusStrateTracker _globalBusStrateTracker;
            readonly Action _messageTask;
            public IMessage Message { get; }
            public Guid MessageId { get; }
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

            public QueuedMessage(IInbox bus, TransportMessage.InComing message, GlobalBusStrateTracker globalBusStrateTracker, Action messageTask)
            {
                Bus = bus;
                MessageId = message.MessageId;
                _globalBusStrateTracker = globalBusStrateTracker;
                _messageTask = messageTask;
                Message = message.DeserializeMessageAndCacheForNextCall();
            }

            public void SetIsExecuting() => IsExecuting = true;
        }
    }
}
