using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.System.Threading;
using Composable.System.Threading.ResourceAccess;

namespace Composable.Messaging.Buses.Implementation
{
    partial class Inbox
    {
        partial class HandlerExecutionEngine
        {
            partial class Coordinator
            {
                readonly ITaskRunner _taskRunner;
                readonly MessageStorage _messageStorage;
                readonly IServiceLocator _serviceLocator;
                readonly IMessageHandlerRegistry _messageHandlerRegistry;
                readonly AwaitableOptimizedThreadShared<NonThreadsafeImplementation> _implementation;

                public Coordinator(IGlobalBusStateTracker globalStateTracker, ITaskRunner taskRunner, MessageStorage messageStorage, IServiceLocator serviceLocator, IMessageHandlerRegistry messageHandlerRegistry)
                {
                    _taskRunner = taskRunner;
                    _messageStorage = messageStorage;
                    _serviceLocator = serviceLocator;
                    _messageHandlerRegistry = messageHandlerRegistry;
                    _implementation = new AwaitableOptimizedThreadShared<NonThreadsafeImplementation>(new NonThreadsafeImplementation(globalStateTracker));
                }

                internal HandlerExecutionTask AwaitExecutableHandlerExecutionTask(IReadOnlyList<IMessageDispatchingRule> dispatchingRules)
                {
                    HandlerExecutionTask? handlerExecutionTask = null;
                    _implementation.Await(implementation => implementation.TryGetDispatchableMessage(dispatchingRules, out handlerExecutionTask));
                    Assert.Result.Assert(handlerExecutionTask != null);
                    return handlerExecutionTask;
                }

                public Task<object?> EnqueueMessageTask(TransportMessage.InComing message) => _implementation.Update(implementation =>
                {
                    var inflightMessage = new HandlerExecutionTask(message, this, _taskRunner, _messageStorage, _serviceLocator, _messageHandlerRegistry);
                    implementation.EnqueueMessageTask(inflightMessage);
                    return inflightMessage.Task;
                });

                void Succeeded(HandlerExecutionTask queuedMessageInformation) => _implementation.Update(implementation => implementation.Succeeded(queuedMessageInformation));

                void Failed(HandlerExecutionTask queuedMessageInformation, Exception exception) => _implementation.Update(implementation => implementation.Failed(queuedMessageInformation, exception));

                class NonThreadsafeImplementation : IExecutingMessagesSnapshot
                {
                    const int MaxConcurrentlyExecutingHandlers = 20;
                    readonly IGlobalBusStateTracker _globalStateTracker;


                    //performance: Split waiting messages into prioritized categories: Exactly once event/command, At most once event/command,  NonTransactional query
                    //don't postpone checking if mutations are allowed to run because we have a ton of queries queued up. Also the queries are likely not allowed to run due to the commands and events!
                    //performance: Use static type caching trick to ensure that we know which rules need to be applied to which messages. Don't check rules that don't apply. (Double dispatching might be required.)
                    public IReadOnlyList<TransportMessage.InComing> AtMostOnceCommands => _executingAtMostOnceCommands;
                    public IReadOnlyList<TransportMessage.InComing> ExactlyOnceCommands => _executingExactlyOnceCommands;
                    public IReadOnlyList<TransportMessage.InComing> ExactlyOnceEvents => _executingExactlyOnceEvents;
                    public IReadOnlyList<TransportMessage.InComing> ExecutingNonTransactionalQueries => _executingNonTransactionalQueries;

                    readonly List<HandlerExecutionTask> _messagesWaitingToExecute = new List<HandlerExecutionTask>();
                    public NonThreadsafeImplementation(IGlobalBusStateTracker globalStateTracker) => _globalStateTracker = globalStateTracker;

                    internal bool TryGetDispatchableMessage(IReadOnlyList<IMessageDispatchingRule> dispatchingRules, [NotNullWhen(true)] out HandlerExecutionTask dispatchable)
                    {
                        dispatchable = null!;
                        if(_executingMessages >= MaxConcurrentlyExecutingHandlers)
                        {
                            return false;
                        }

                        dispatchable = _messagesWaitingToExecute
                           .FirstOrDefault(queuedTask => dispatchingRules.All(rule => rule.CanBeDispatched(this, queuedTask.TransportMessage)));

                        if (dispatchable == null)
                        {
                            return false;
                        }

                        Dispatching(dispatchable);
                        return true;
                    }

                    public void EnqueueMessageTask(HandlerExecutionTask message) => _messagesWaitingToExecute.Add(message);

                    internal void Succeeded(HandlerExecutionTask queuedMessageInformation) => DoneDispatching(queuedMessageInformation);

                    internal void Failed(HandlerExecutionTask queuedMessageInformation, Exception exception) => DoneDispatching(queuedMessageInformation, exception);


                    void Dispatching(HandlerExecutionTask dispatchable)
                    {
                        _executingMessages++;

                        switch(dispatchable.TransportMessage.MessageTypeEnum)
                        {
                            case TransportMessage.TransportMessageType.ExactlyOnceEvent:
                                _executingExactlyOnceEvents.Add(dispatchable.TransportMessage);
                                break;
                            case TransportMessage.TransportMessageType.AtMostOnceCommand:
                                _executingAtMostOnceCommands.Add(dispatchable.TransportMessage);
                                break;
                            case TransportMessage.TransportMessageType.ExactlyOnceCommand:
                                _executingExactlyOnceCommands.Add(dispatchable.TransportMessage);
                                break;
                            case TransportMessage.TransportMessageType.NonTransactionalQuery:
                                _executingNonTransactionalQueries.Add(dispatchable.TransportMessage);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        _messagesWaitingToExecute.Remove(dispatchable);
                    }

                    void DoneDispatching(HandlerExecutionTask doneExecuting, Exception? exception = null)
                    {
                        _executingMessages--;

                        switch(doneExecuting.TransportMessage.MessageTypeEnum)
                        {
                            case TransportMessage.TransportMessageType.ExactlyOnceEvent:
                                _executingExactlyOnceEvents.Remove(doneExecuting.TransportMessage);
                                break;
                            case TransportMessage.TransportMessageType.AtMostOnceCommand:
                                _executingAtMostOnceCommands.Remove(doneExecuting.TransportMessage);
                                break;
                            case TransportMessage.TransportMessageType.ExactlyOnceCommand:
                                _executingExactlyOnceCommands.Remove(doneExecuting.TransportMessage);
                                break;
                            case TransportMessage.TransportMessageType.NonTransactionalQuery:
                                _executingNonTransactionalQueries.Remove(doneExecuting.TransportMessage);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        _globalStateTracker.DoneWith(doneExecuting.MessageId, exception);
                    }

                    int _executingMessages;
                    readonly List<TransportMessage.InComing> _executingExactlyOnceCommands = new List<TransportMessage.InComing>();
                    readonly List<TransportMessage.InComing> _executingAtMostOnceCommands = new List<TransportMessage.InComing>();
                    readonly List<TransportMessage.InComing> _executingExactlyOnceEvents = new List<TransportMessage.InComing>();
                    readonly List<TransportMessage.InComing> _executingNonTransactionalQueries = new List<TransportMessage.InComing>();
                }
            }
        }
    }
}
