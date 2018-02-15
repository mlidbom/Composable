using System;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.System.Threading;
using Composable.System.Transactions;

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
                    internal readonly TaskCompletionSource<object> _taskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                    internal readonly TransportMessage.InComing TransportMessage;
                    readonly Coordinator _coordinator;
                    readonly Func<object, object> _messageTask;
                    readonly ITaskRunner _taskRunner;
                    readonly MessageStorage _messageStorage;
                    readonly IServiceLocator _serviceLocator;
                    // ReSharper disable once UnusedMember.Local
                    public BusApi.IMessage DeserializeMessageAndCache() => TransportMessage.DeserializeMessageAndCacheForNextCall();
                    public Guid MessageId { get; }

                    public void Run()
                    {
                        var message = DeserializeMessageAndCache();
                        _taskRunner.RunAndCrashProcessIfTaskThrows(() =>
                        {
                            try
                            {
                                var result = message is BusApi.IRequireTransactionalReceiver
                                                    ? _serviceLocator.ExecuteTransactionInIsolatedScope(() => _messageTask(message))
                                                    : _serviceLocator.ExecuteInIsolatedScope(() => _messageTask(message));

                                _taskCompletionSource.SetResult(result);

                                if(message is BusApi.Remotable.IAtMostOnceMessage)
                                {
                                    _messageStorage.MarkAsHandled(TransportMessage);
                                }
                                _coordinator.Succeeded(this);
                            }
                            catch(Exception exception)
                            {
                                //Mark as failed in storage
                                _taskCompletionSource.SetException(exception);
                                _coordinator.Failed(this, exception);
                            }
                        });
                    }

                    public QueuedHandlerExecutionTask(TransportMessage.InComing transportMessage, Coordinator coordinator, Func<object, object> messageTask, ITaskRunner taskRunner, MessageStorage messageStorage, IServiceLocator serviceLocator)
                    {
                        MessageId = transportMessage.MessageId;
                        TransportMessage = transportMessage;
                        _coordinator = coordinator;
                        _messageTask = messageTask;
                        _taskRunner = taskRunner;
                        _messageStorage = messageStorage;
                        _serviceLocator = serviceLocator;
                    }
                }
            }
        }
    }
}
