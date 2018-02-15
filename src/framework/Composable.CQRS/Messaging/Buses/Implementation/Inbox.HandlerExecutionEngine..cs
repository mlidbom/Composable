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
                if(message.Is<BusApi.IQuery>())
                    return DispatchQueryAsync(message);
                if(message.Is<BusApi.Remotable.AtMostOnce.ICommand>())
                    return DispatchAtMostOnceCommandAsync(message);
                else if(message.Is<BusApi.Remotable.ExactlyOnce.IEvent>())
                    return DispatchExactlyOnceEventAsync(message);
                if(message.Is<BusApi.Remotable.ExactlyOnce.ICommand>())
                        return DispatchExactlyOnceCommandAsync(message);
                else
                    throw new ArgumentOutOfRangeException();

            }

            void AwaitDispatchableMessageThread()
            {
                try
                {
                    while(true)
                    {
                        var dispatchableMessage = _coordinator.AwaitDispatchableMessage(_dispatchingRules);
                        dispatchableMessage.Run();
                    }
                }
                catch(Exception exception) when(exception is OperationCanceledException || exception is ThreadInterruptedException) {}
            }

            async Task<object> DispatchQueryAsync(TransportMessage.InComing message)
            {
                var handler = _handlerRegistry.GetQueryHandler(message.MessageType);

                var taskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

                _coordinator.EnqueueMessageTask(
                    message,
                    () => _serviceLocator.ExecuteInIsolatedScope(() =>
                    {
                        try
                        {
                            var query = (BusApi.IQuery)message.DeserializeMessageAndCacheForNextCall();
                            var result = handler(query);
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
                var eventHandlers = _handlerRegistry.GetEventHandlers(message.MessageType);
                var taskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

                _coordinator.EnqueueMessageTask(
                    message,
                    () =>
                    {
                        try
                        {
                            var @event = (BusApi.Remotable.ExactlyOnce.IEvent)message.DeserializeMessageAndCacheForNextCall();
                            _serviceLocator.ExecuteTransactionInIsolatedScope(() => eventHandlers.ForEach(handler => handler(@event)));
                            _storage.MarkAsHandled(message);
                            taskCompletionSource.SetResult(null);
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

                _coordinator.EnqueueMessageTask(
                    message,
                    () =>
                    {
                        try
                        {
                            var exactlyOnceCommand = (BusApi.Remotable.ExactlyOnce.ICommand)message.DeserializeMessageAndCacheForNextCall();
                            var result = _serviceLocator.ExecuteTransactionInIsolatedScope(() => handler(exactlyOnceCommand));
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

                _coordinator.EnqueueMessageTask(
                    message,
                    () =>
                    {
                        try
                        {
                            var atMostOnceCommand = (BusApi.Remotable.AtMostOnce.ICommand)message.DeserializeMessageAndCacheForNextCall();
                            var result = _serviceLocator.ExecuteTransactionInIsolatedScope(() => handler(atMostOnceCommand));
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
