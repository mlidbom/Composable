using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Composable.Refactoring.Naming;
using Composable.System;
using Composable.System.Threading.ResourceAccess;

namespace Composable.Messaging.Buses.Implementation
{
    partial class Inbox
    {
        partial class HandlerExecutionEngine
        {
            class Coordinator
            {
                readonly IGlobalBusStateTracker _globalStateTracker;
                readonly ITypeIdMapper _typeMapper;
                readonly List<QueuedMessage>    _messagesWaitingToExecute = new List<QueuedMessage>();

                //Todo: It is never OK for this class to block for a significant amount of time. So make that explicit with a really strict timeout on all operations waiting for access.
                //Currently we cannot make the timeout really strict because it does time out....
                readonly IResourceGuard _guard = ResourceGuard.WithTimeout(100.Milliseconds());
                public Coordinator(IGlobalBusStateTracker globalStateTracker, ITypeIdMapper typeMapper)
                {
                    _globalStateTracker = globalStateTracker;
                    _typeMapper = typeMapper;
                }

                internal QueuedMessage AwaitDispatchableMessage(IReadOnlyList<IMessageDispatchingRule> dispatchingRules)
                {
                    using(var @lock = _guard.AwaitExclusiveLock())
                    {
                        QueuedMessage result;
                        do
                        {
                            var executingMessages = _messagesWaitingToExecute.Where(@this => @this.IsExecuting).Select(queued => queued.Message).ToList();

                            result = _messagesWaitingToExecute
                                    .Where(queuedMessage => !queuedMessage.IsExecuting)
                                    .FirstOrDefault(queuedTask => dispatchingRules.All(rule => rule.CanBeDispatched(executingMessages, queuedTask.Message)));

                            if(result == null)
                            {
                                @lock.ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(7.Days());
                            }
                        } while(result == null);

                        result.SetIsExecuting();
                        return result;
                    }
                }

                public void EnqueueMessageTask(TransportMessage.InComing message, Action messageTask) => _guard.Update(() =>
                {
                    var inflightMessage = new QueuedMessage(message, this, messageTask, _typeMapper);
                    _messagesWaitingToExecute.Add(inflightMessage);
                    return inflightMessage;
                });

                void Succeeded(QueuedMessage queuedMessageInformation) => _guard.Update(() => DoneDispatching(queuedMessageInformation));

                void Failed(QueuedMessage queuedMessageInformation, Exception exception) => _guard.Update(() => DoneDispatching(queuedMessageInformation, exception));

                void DoneDispatching(QueuedMessage queuedMessageInformation, Exception exception = null)
                {
                    _globalStateTracker.DoneWith(queuedMessageInformation.MessageId, exception);
                    _messagesWaitingToExecute.Remove(queuedMessageInformation);
                }

                internal class QueuedMessage
                {
                    readonly Coordinator _coordinator;
                    readonly Action      _messageTask;
                    public   IMessage    Message     { get; }
                    public   Guid        MessageId   { get; }
                    public   bool        IsExecuting { get; private set; }

                    public void Run()
                    {
                        Task.Run(() =>
                        {
                            try
                            {
                                _messageTask();
                                _coordinator.Succeeded(this);
                            }
                            catch(Exception exception)
                            {
                                _coordinator.Failed(this, exception);
                            }
                        });
                    }

                    public QueuedMessage(TransportMessage.InComing message, Coordinator coordinator, Action messageTask, ITypeIdMapper typeMapper)
                    {
                        MessageId    = message.MessageId;
                        _coordinator = coordinator;
                        _messageTask = messageTask;
                        Message      = message.DeserializeMessageAndCacheForNextCall(typeMapper);
                    }

                    public void SetIsExecuting() => IsExecuting = true;
                }
            }
        }
    }
}
