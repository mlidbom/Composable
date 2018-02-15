using System;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.System;
using Composable.System.Linq;
using Composable.System.Threading;

namespace Composable.Messaging.Buses.Implementation
{
    partial class Inbox
    {
        partial class HandlerExecutionEngine
        {
            partial class Coordinator
            {
                // ReSharper disable once MemberCanBePrivate.Local Resharper is just confused....
                internal class QueuedHandlerExecutionTask
                {
                    readonly TaskCompletionSource<object> _taskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                    internal readonly TransportMessage.InComing TransportMessage;
                    readonly Coordinator _coordinator;
                    readonly Func<object, object> _messageTask;
                    readonly ITaskRunner _taskRunner;
                    readonly MessageStorage _messageStorage;
                    readonly IServiceLocator _serviceLocator;
                    readonly IMessageHandlerRegistry _handlerRegistry;

                    internal Task<object> Task => _taskCompletionSource.Task;
                    public Guid MessageId { get; }

                    public void Execute()
                    {
                        var message = TransportMessage.DeserializeMessageAndCacheForNextCall();
                        _taskRunner.RunAndCrashProcessIfTaskThrows(() =>
                        {
                            var retryPolicy = new DefaultRetryPolicy(message);

                            while(true)
                            {
                                try
                                {
                                    var result = message is BusApi.IRequireTransactionalReceiver
                                                     ? _serviceLocator.ExecuteTransactionInIsolatedScope(() =>
                                                     {
                                                         var innerResult = _messageTask(message);
                                                         if(message is BusApi.Remotable.IAtMostOnceMessage)
                                                         {
                                                             _messageStorage.MarkAsHandled(TransportMessage);
                                                         }

                                                         return innerResult;
                                                     })
                                                     : _serviceLocator.ExecuteInIsolatedScope(() => _messageTask(message));

                                    _taskCompletionSource.SetResult(result);
                                    _coordinator.Succeeded(this);
                                    return;
                                }
                                catch(Exception exception)
                                {
                                    if(!retryPolicy.Retry(exception))
                                    {
                                        if(message is BusApi.Remotable.IAtMostOnceMessage)
                                        {
                                            _messageStorage.MarkAsHandled(TransportMessage);
                                        }

                                        _taskCompletionSource.SetException(exception);
                                        _coordinator.Failed(this, exception);
                                        return;
                                    }
                                }
                            }
                        });
                    }

                    public QueuedHandlerExecutionTask(TransportMessage.InComing transportMessage, Coordinator coordinator, ITaskRunner taskRunner, MessageStorage messageStorage, IServiceLocator serviceLocator, IMessageHandlerRegistry handlerRegistry)
                    {
                        MessageId = transportMessage.MessageId;
                        TransportMessage = transportMessage;
                        _coordinator = coordinator;
                        _taskRunner = taskRunner;
                        _messageStorage = messageStorage;
                        _serviceLocator = serviceLocator;
                        _handlerRegistry = handlerRegistry;
                        _messageTask = CreateMessageTask();
                    }

                    Func<object, object> CreateMessageTask()
                    {
                        switch(TransportMessage.MessageTypeEnum)
                        {
                            case Implementation.TransportMessage.TransportMessageType.ExactlyOnceEvent:
                                return message =>
                                {
                                    _handlerRegistry.GetEventHandlers(message.GetType()).ForEach(handler => handler((BusApi.Remotable.ExactlyOnce.IEvent)message));
                                    return null;
                                };
                            case Implementation.TransportMessage.TransportMessageType.AtMostOnceCommand:
                                return message => _handlerRegistry.GetCommandHandler(message.GetType())((BusApi.Remotable.AtMostOnce.ICommand)message);
                            case Implementation.TransportMessage.TransportMessageType.ExactlyOnceCommand:
                                return message => _handlerRegistry.GetCommandHandler(message.GetType())((BusApi.Remotable.ExactlyOnce.ICommand)message);
                            case Implementation.TransportMessage.TransportMessageType.NonTransactionalQuery:
                                return actualMessage => _handlerRegistry.GetQueryHandler(actualMessage.GetType())((BusApi.IQuery)actualMessage);
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            }
        }
    }
}
