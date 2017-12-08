using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.System;
using Composable.System.Collections.Collections;
using Composable.System.Linq;
using Composable.System.Threading.ResourceAccess;

namespace Composable.Messaging.Buses.Implementation
{
    class GlobalBusStrateTracker : IGlobalBusStrateTracker
    {
        readonly List<QueuedMessage> _queuedMessages = new List<QueuedMessage>();
        readonly Dictionary<Guid, InFlightMessage> _inflightMessages = new Dictionary<Guid, InFlightMessage>();

        //Todo: It is never OK for this class to block for a significant amount of time. So make that explicit with a really strict timeout on all operations waiting for access.
        //Currently we cannot make the timeout really strict because it does time out....
        readonly IResourceGuard _guard = ResourceGuard.WithTimeout(100.Milliseconds());

        readonly Dictionary<IInbox, IList<Exception>> _busExceptions = new Dictionary<IInbox, IList<Exception>>();

        public IReadOnlyList<Exception> GetExceptionsFor(IInbox bus) => _guard.Update(() => _busExceptions.GetOrAdd(bus, () => new List<Exception>()).ToList());

        public IQueuedMessage AwaitDispatchableMessage(IInbox bus, IReadOnlyList<IMessageDispatchingRule> dispatchingRules)
        {
            using(var @lock = _guard.AwaitExclusiveLock())
            {
                QueuedMessage result;
                do
                {
                    var snapshot = new GlobalBusStateSnapshot(bus, _queuedMessages.ToList(), _inflightMessages.Values.Select(msg => msg.Message).ToList());

                    result = _queuedMessages
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

        public void SendingMessageOnTransport(TransportMessage.OutGoing transportMessage, IMessage message) => _guard.Update(() =>
        {
            var inFlightMessage = _inflightMessages.GetOrAdd(transportMessage.MessageId, () => new InFlightMessage(transportMessage));
            inFlightMessage.RemainingReceivers++;
        });

        public void EnqueueMessageTask(IInbox bus, TransportMessage.InComing message, Action messageTask) => _guard.Update(() =>
        {
            var inflightMessage = new QueuedMessage(bus, message, this, messageTask);
            _queuedMessages.Add(inflightMessage);
            return inflightMessage;
        });

        public void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride)
            => _guard.AwaitCondition(timeout: timeoutOverride ?? 30.Seconds(),
                                     condition: () => _queuedMessages.None() && _inflightMessages.None());

        void Succeeded(QueuedMessage queuedMessageInformation) => _guard.Update(() => DoneDispatching(queuedMessageInformation));

        void Failed(QueuedMessage queuedMessageInformation, Exception exception) => _guard.Update(() =>
        {
            _busExceptions.GetOrAdd(queuedMessageInformation.Bus, () => new List<Exception>()).Add(exception);
            DoneDispatching(queuedMessageInformation);
        });

        void DoneDispatching(QueuedMessage queuedMessageInformation)
        {
            _queuedMessages.Remove(queuedMessageInformation);
            var inFlightMessage = _inflightMessages[queuedMessageInformation.MessageId];
            inFlightMessage.RemainingReceivers--;
            if(inFlightMessage.RemainingReceivers == 0)
            {
                _inflightMessages.Remove(queuedMessageInformation.MessageId);
            }
        }

        class InFlightMessage
        {
            public InFlightMessage(TransportMessage.OutGoing message) => Message = message;
            public int RemainingReceivers { get; set; }
            public TransportMessage.OutGoing Message { get; set; }
        }

        class GlobalBusStateSnapshot : IGlobalBusStateSnapshot
        {
            public GlobalBusStateSnapshot(IInbox bus, IReadOnlyList<QueuedMessage> queuedMessages, List<TransportMessage.OutGoing> inFlightMessages)
            {
                var bus1 = bus;
                MessagesQueuedForExecution = queuedMessages;
                InFlightMessages = inFlightMessages;
                MessagesQueuedForExecutionLocally = queuedMessages.Where(message => message.Bus == bus1 && message.IsExecuting).ToList();
            }

            public IReadOnlyList<IQueuedMessageInformation> MessagesQueuedForExecution { get; }
            public IReadOnlyList<IQueuedMessage> MessagesQueuedForExecutionLocally { get; }
            public IReadOnlyList<TransportMessage.OutGoing> InFlightMessages { get; }
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
