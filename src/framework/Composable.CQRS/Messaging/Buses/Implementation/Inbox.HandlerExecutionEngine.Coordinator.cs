using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Refactoring.Naming;
using Composable.System.Threading;
using Composable.System.Threading.ResourceAccess;

namespace Composable.Messaging.Buses.Implementation
{
    partial class Inbox
    {
        partial class HandlerExecutionEngine
        {
            class Coordinator
            {
                readonly ITaskRunner _taskRunner;
                readonly AwaitableOptimizedThreadShared<NonThreadsafeImplementation> _implementation;

                public Coordinator(IGlobalBusStateTracker globalStateTracker, ITypeMapper typeMapper, ITaskRunner taskRunner)
                {
                    _taskRunner = taskRunner;
                    _implementation = new AwaitableOptimizedThreadShared<NonThreadsafeImplementation>(new NonThreadsafeImplementation(globalStateTracker, typeMapper));
                }

                internal QueuedMessage AwaitDispatchableMessage(IReadOnlyList<IMessageDispatchingRule> dispatchingRules)
                {
                    QueuedMessage message = null;
                    _implementation.Await(implementation => implementation.TryGetDispatchableMessage(dispatchingRules, out message));
                    return message;
                }

                public void EnqueueMessageTask(TransportMessage.InComing message, Action messageTask) => _implementation.Update(implementation =>
                {
                    var inflightMessage = new QueuedMessage(message, this, messageTask, implementation.TypeMapper, _taskRunner);
                    implementation.EnqueueMessageTask(inflightMessage);
                });

                void Succeeded(QueuedMessage queuedMessageInformation) => _implementation.Update(implementation => implementation.Succeeded(queuedMessageInformation));

                void Failed(QueuedMessage queuedMessageInformation, Exception exception) => _implementation.Update(implementation => implementation.Failed(queuedMessageInformation, exception));



                class NonThreadsafeImplementation
                {
                    const int MaxConcurrentlyExecutingHandlers = 20;
                    readonly IGlobalBusStateTracker _globalStateTracker;
                    internal readonly ITypeMapper TypeMapper;
                    readonly List<BusApi.IMessage> _executingMessages = new List<BusApi.IMessage>();
                    readonly List<QueuedMessage> _messagesWaitingToExecute = new List<QueuedMessage>();
                    public NonThreadsafeImplementation(IGlobalBusStateTracker globalStateTracker, ITypeMapper typeMapper)
                    {
                        _globalStateTracker = globalStateTracker;
                        TypeMapper = typeMapper;
                    }

                    internal bool TryGetDispatchableMessage(IReadOnlyList<IMessageDispatchingRule> dispatchingRules, out QueuedMessage dispatchable)
                    {
                        dispatchable = null;
                        if(_executingMessages.Count >= MaxConcurrentlyExecutingHandlers)
                        {
                            return false;
                        }

                        dispatchable = _messagesWaitingToExecute
                                .FirstOrDefault(queuedTask => dispatchingRules.All(rule => rule.CanBeDispatched(_executingMessages, queuedTask.Message)));

                        if(dispatchable == null)
                        {
                            return false;
                        }

                        _executingMessages.Add(dispatchable.Message);
                        _messagesWaitingToExecute.Remove(dispatchable);
                        return true;
                    }

                    public void EnqueueMessageTask(QueuedMessage message) => _messagesWaitingToExecute.Add(message);

                    internal void Succeeded(QueuedMessage queuedMessageInformation) => DoneDispatching(queuedMessageInformation);

                    internal void Failed(QueuedMessage queuedMessageInformation, Exception exception) => DoneDispatching(queuedMessageInformation, exception);

                    void DoneDispatching(QueuedMessage queuedMessageInformation, Exception exception = null)
                    {
                        _executingMessages.Remove(queuedMessageInformation.Message);
                        _globalStateTracker.DoneWith(queuedMessageInformation.MessageId, exception);
                    }
                }


                internal class QueuedMessage
                {
                    readonly Coordinator _coordinator;
                    readonly Action      _messageTask;
                    readonly ITaskRunner _taskRunner;
                    public   BusApi.IMessage    Message     { get; }
                    public   Guid        MessageId   { get; }

                    public void Run()
                    {
                        _taskRunner.RunAndCrashProcessIfTaskThrows(() =>
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

                    public QueuedMessage(TransportMessage.InComing message, Coordinator coordinator, Action messageTask, ITypeMapper typeMapper, ITaskRunner taskRunner)
                    {
                        MessageId    = message.MessageId;
                        _coordinator = coordinator;
                        _messageTask = messageTask;
                        _taskRunner = taskRunner;
                        Message      = message.DeserializeMessageAndCacheForNextCall(typeMapper);
                    }
                }
            }
        }
    }
}
