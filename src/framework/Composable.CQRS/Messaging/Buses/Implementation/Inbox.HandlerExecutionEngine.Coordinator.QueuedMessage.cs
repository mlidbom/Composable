using System;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.GenericAbstractions;
using Composable.System;
using Composable.System.Linq;
using Composable.System.Threading;
using Composable.System.Threading.Tasks;

namespace Composable.Messaging.Buses.Implementation
{
    partial class Inbox
    {
        partial class HandlerExecutionEngine
        {
            partial class Coordinator
            {
                // ReSharper disable once MemberCanBePrivate.Local Resharper is just confused....
                internal class HandlerExecutionTask
                {
                    readonly AsyncTaskCompletionSource<object?> _taskCompletionSource = new AsyncTaskCompletionSource<object?>();
                    internal readonly TransportMessage.InComing TransportMessage;
                    readonly Coordinator _coordinator;
                    readonly Func<object, object?> _messageTask;
                    readonly ITaskRunner _taskRunner;
                    readonly IMessageStorage _messageStorage;
                    readonly IServiceLocator _serviceLocator;
                    readonly IMessageHandlerRegistry _handlerRegistry;

                    internal Task<object?> Task => _taskCompletionSource.Task;
                    public Guid MessageId { get; }

                    public void Execute()
                    {
                        var message = TransportMessage.DeserializeMessageAndCacheForNextCall();
                        _taskRunner.RunAndSurfaceExceptions(() =>
                        {
                            var retryPolicy = new DefaultRetryPolicy(message);

                            while(true)
                            {
                                try
                                {
                                    var result = message is MessageTypes.IRequireTransactionalReceiver
                                                     ? _serviceLocator.ExecuteTransactionInIsolatedScope(() =>
                                                     {
                                                         var innerResult = _messageTask(message);
                                                         if(message is MessageTypes.Remotable.IAtMostOnceMessage)
                                                         {
                                                             _messageStorage.MarkAsSucceeded(TransportMessage);
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
                                    if(message is MessageTypes.Remotable.IAtMostOnceMessage)
                                    {
                                        _messageStorage.RecordException(TransportMessage, exception);
                                    }

                                    if(!retryPolicy.TryAwaitNextRetryTimeForException(exception))
                                    {
                                        if(message is MessageTypes.Remotable.IAtMostOnceMessage)
                                        {
                                            _messageStorage.MarkAsFailed(TransportMessage);
                                        }

                                        _taskCompletionSource.SetException(exception);
                                        _coordinator.Failed(this, exception);
                                        return;
                                    }
                                }
                            }
                        });
                    }

                    public HandlerExecutionTask(TransportMessage.InComing transportMessage, Coordinator coordinator, ITaskRunner taskRunner, IMessageStorage messageStorage, IServiceLocator serviceLocator, IMessageHandlerRegistry handlerRegistry)
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

                    //Refactor: Switching should not be necessary. See also inbox.
                    Func<object, object?> CreateMessageTask() =>
                        TransportMessage.MessageTypeEnum switch
                        {
                            Implementation.TransportMessage.TransportMessageType.ExactlyOnceEvent => message =>
                            {
                                var eventHandlers = _handlerRegistry.GetEventHandlers(message.GetType());
                                eventHandlers.ForEach(handler => handler((MessageTypes.Remotable.ExactlyOnce.IEvent)message));
                                return null;
                            },
                            Implementation.TransportMessage.TransportMessageType.AtMostOnceCommandWithReturnValue => message =>
                            {
                                var commandHandler = _handlerRegistry.GetCommandHandlerWithReturnValue(message.GetType());
                                return commandHandler((MessageTypes.Remotable.AtMostOnce.ICommand)message);
                            },
                            Implementation.TransportMessage.TransportMessageType.AtMostOnceCommand => message =>
                            {
                                var commandHandler = _handlerRegistry.GetCommandHandler(message.GetType());
                                commandHandler((MessageTypes.Remotable.AtMostOnce.ICommand)message);
                                return Unit.Instance; //Todo:Properly handle commands with and without return values
                            },
                            Implementation.TransportMessage.TransportMessageType.ExactlyOnceCommand => message =>
                            {
                                var commandHandler = _handlerRegistry.GetCommandHandler(message.GetType());
                                commandHandler((MessageTypes.Remotable.ExactlyOnce.ICommand)message);
                                return Unit.Instance;//Todo:Properly handle commands with and without return values
                            },
                            Implementation.TransportMessage.TransportMessageType.NonTransactionalQuery => actualMessage =>
                            {
                                var queryHandler = _handlerRegistry.GetQueryHandler(actualMessage.GetType());
                                return queryHandler((MessageTypes.IQuery)actualMessage);
                            },
                            _ => throw new ArgumentOutOfRangeException()
                        };
                }
            }
        }
    }
}
