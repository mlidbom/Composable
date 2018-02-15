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
                _coordinator = new Coordinator(globalStateTracker, taskRunner, storage, serviceLocator);
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
                return await _coordinator.EnqueueMessageTask(message, actualMessage => handler((BusApi.IQuery)actualMessage));
            }

            async Task<object> DispatchExactlyOnceEventAsync(TransportMessage.InComing transportMessage) =>
                await _coordinator.EnqueueMessageTask(
                    transportMessage,
                    message =>
                    {
                        var @event = (BusApi.Remotable.ExactlyOnce.IEvent)message;
                        _handlerRegistry.GetEventHandlers(transportMessage.MessageType).ForEach(handler => handler(@event));
                        return null;
                    });

            async Task<object> DispatchExactlyOnceCommandAsync(TransportMessage.InComing transportMessage) =>
                await _coordinator.EnqueueMessageTask(
                    transportMessage,
                    message =>
                    {
                        var exactlyOnceCommand = (BusApi.Remotable.ExactlyOnce.ICommand)message;
                        return _handlerRegistry.GetCommandHandler(transportMessage.MessageType)(exactlyOnceCommand);
                    });

            async Task<object> DispatchAtMostOnceCommandAsync(TransportMessage.InComing message) =>
                await _coordinator.EnqueueMessageTask(
                    message,
                    actualMessage =>
                    {
                        var atMostOnceCommand = (BusApi.Remotable.AtMostOnce.ICommand)actualMessage;
                        return _handlerRegistry.GetCommandHandler(message.MessageType)(atMostOnceCommand);
                    });

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
