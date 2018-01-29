using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.System.Linq;
using Composable.System.Threading;

namespace Composable.Messaging.Buses.Implementation
{
    partial class Inbox
    {
        partial class HandlerExecutionEngine
        {
            readonly IMessageHandlerRegistry _handlerRegistry;
            readonly IServiceLocator _serviceLocator;
            readonly MessageStorage _storage;
            Thread _awaitDispatchableMessageThread;
            CancellationTokenSource _cancellationTokenSource;

            readonly IReadOnlyList<IMessageDispatchingRule> _dispatchingRules = new List<IMessageDispatchingRule>()
                                                                                {
                                                                                    new QueriesExecuteAfterAllCommandsAndEventsAreDone(),
                                                                                    new CommandsAndEventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint()
                                                                                };
            readonly Coordinator _coordinator;

            public HandlerExecutionEngine(IGlobalBusStateTracker globalStateTracker,
                                          IMessageHandlerRegistry handlerRegistry,
                                          IServiceLocator serviceLocator,
                                          MessageStorage storage,
                                          ITaskRunner taskRunner)
            {
                _handlerRegistry = handlerRegistry;
                _serviceLocator = serviceLocator;
                _storage = storage;
                _coordinator =  new Coordinator(globalStateTracker, taskRunner);
            }


            internal Task<object> Enqueue(TransportMessage.InComing message)
            {
                if(typeof(BusApi.IQuery).IsAssignableFrom(message.MessageType))
                    return DispatchQueryAsync(message);
                if(typeof(BusApi.Remotable.AtMostOnce.ICommand).IsAssignableFrom(message.MessageType))
                    return DispatchAtMostOnceCommandAsync(message);
                else if(typeof(BusApi.Remotable.ExactlyOnce.IEvent).IsAssignableFrom(message.MessageType))
                    return DispatchExactlyOnceEventAsync(message);
                if(typeof(BusApi.Remotable.ExactlyOnce.ICommand).IsAssignableFrom(message.MessageType))
                        return DispatchExactlyOnceCommandAsync(message);
                else
                    throw new ArgumentOutOfRangeException();

            }

            void AwaitDispatchableMessageThread()
            {
                while(!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        var dispatchableMessage = _coordinator.AwaitDispatchableMessage(_dispatchingRules);
                        dispatchableMessage.Run();
                    }
                    catch(Exception exception) when(exception is OperationCanceledException || exception is ThreadInterruptedException)
                    {
                        return;
                    }
                }
            }

            async Task<object> DispatchQueryAsync(TransportMessage.InComing message)
            {
                var handler = _handlerRegistry.GetQueryHandler(message.MessageType);

                var taskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

                _coordinator.EnqueueMessageTask(message, () => _serviceLocator.ExecuteInIsolatedScope(() =>
                {
                    try
                    {
                        var result = handler((BusApi.IQuery)message.DeserializeMessageAndCacheForNextCall());
                        taskCompletionSource.SetResult(result);
                    }
                    catch(Exception exception)
                    {
                        taskCompletionSource.SetException(exception);
                        throw;
                    }
                }));

                return await taskCompletionSource.Task.NoMarshalling();
            }

            async Task<object> DispatchExactlyOnceEventAsync(TransportMessage.InComing message)
            {
                var handler = _handlerRegistry.GetEventHandlers(message.MessageType);
                var taskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

                _coordinator.EnqueueMessageTask(message,
                                                () =>
                                                {
                                                    try
                                                    {
                                                        _serviceLocator.ExecuteTransactionInIsolatedScope(() => handler.ForEach(action: @this => @this((BusApi.Remotable.ExactlyOnce.IEvent)message.DeserializeMessageAndCacheForNextCall())));
                                                        _storage.MarkAsHandled(message);
                                                        taskCompletionSource.SetResult(result: null);
                                                    }
                                                    catch(Exception exception)
                                                    {
                                                        taskCompletionSource.SetException(exception);
                                                        throw;
                                                    }
                                                });

                return await taskCompletionSource.Task.NoMarshalling();
            }

            async Task<object> DispatchExactlyOnceCommandAsync(TransportMessage.InComing message)
            {
                var handler = _handlerRegistry.GetCommandHandler(message.MessageType);

                var taskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

                _coordinator.EnqueueMessageTask(message,
                                                () =>
                                                {
                                                    try
                                                    {
                                                        var result = _serviceLocator.ExecuteTransactionInIsolatedScope(() => handler((BusApi.Remotable.ExactlyOnce.ICommand)message.DeserializeMessageAndCacheForNextCall()));
                                                        _storage.MarkAsHandled(message);
                                                        taskCompletionSource.SetResult(result);
                                                    }
                                                    catch(Exception exception)
                                                    {
                                                        taskCompletionSource.SetException(exception);
                                                        throw;
                                                    }
                                                });

                return await taskCompletionSource.Task.NoMarshalling();
            }

            async Task<object> DispatchAtMostOnceCommandAsync(TransportMessage.InComing message)
            {
                var handler = _handlerRegistry.GetCommandHandler(message.MessageType);

                var taskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

                _coordinator.EnqueueMessageTask(message, () =>
                {
                    try
                    {
                        var result = _serviceLocator.ExecuteTransactionInIsolatedScope(() => handler((BusApi.Remotable.AtMostOnce.ICommand)message.DeserializeMessageAndCacheForNextCall()));
                        taskCompletionSource.SetResult(result);
                    }
                    catch(Exception exception)
                    {
                        taskCompletionSource.SetException(exception);
                        throw;
                    }
                });

                return await taskCompletionSource.Task.NoMarshalling();
            }

            public void Start()
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _awaitDispatchableMessageThread = new Thread(AwaitDispatchableMessageThread)
                                     {
                                         Name = nameof(AwaitDispatchableMessageThread),
                                         Priority = ThreadPriority.AboveNormal
                                     };
                _awaitDispatchableMessageThread.Start();
            }

            public void Stop()
            {
                _cancellationTokenSource.Cancel();
                _awaitDispatchableMessageThread.InterruptAndJoin();
                _awaitDispatchableMessageThread = null;
            }
        }
    }
}
