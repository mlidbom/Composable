using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.NewtonSoft;
using Composable.System;
using Composable.System.Linq;
using Composable.System.Reflection;
using Composable.System.Threading;
using Composable.System.Threading.ResourceAccess;
using Composable.System.Transactions;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;

namespace Composable.Messaging.Buses.Implementation
{
    class Inbox : IInbox, IDisposable
    {
        readonly IServiceLocator _serviceLocator;
        readonly IGlobalBusStrateTracker _globalStateTracker;
        readonly IMessageHandlerRegistry _handlerRegistry;

        readonly IGuardedResource _guardedResource = GuardedResource.WithTimeout(1.Seconds());

        readonly CancellationTokenSource _cancellationTokenSource;

        readonly IReadOnlyList<IMessageDispatchingRule> _dispatchingRules = new List<IMessageDispatchingRule>()
                                                                            {
                                                                                new QueriesExecuteAfterAllCommandsAndEventsAreDone(),
                                                                                new CommandsAndEventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint()
                                                                            };
        bool _running;
        readonly Thread _messagePumpThread;
        readonly string _address;
        RouterSocket _responseSocket;

        public IReadOnlyList<Exception> ThrownExceptions => _globalStateTracker.GetExceptionsFor(this);
        NetMQPoller _poller;

        public Inbox(IServiceLocator serviceLocator, IGlobalBusStrateTracker globalStateTracker, IMessageHandlerRegistry handlerRegistry)
        {
            _address = $"inproc://{Guid.NewGuid()}";
            _serviceLocator = serviceLocator;
            _globalStateTracker = globalStateTracker;
            _handlerRegistry = handlerRegistry;
            _cancellationTokenSource = new CancellationTokenSource();
            _messagePumpThread = new Thread(MessagePumpThread)
                                 {
                                     Name = "_MessagePump",
                                     Priority = ThreadPriority.AboveNormal
                                 };

        }


        public void Start() => _guardedResource.Update(action: () =>
        {
            Contract.Invariant.Assert(!_running);
            _running = true;

            _responseSocket = new RouterSocket();
            _responseSocket.Options.Linger = 0.Milliseconds();
            _responseSocket.Bind(_address);
            _responseSocket.ReceiveReady += HandleIncomingMessage;
            _poller = new NetMQPoller() {_responseSocket};
            _poller.RunAsync();

            _messagePumpThread.Start();
        });

        void HandleIncomingMessage(object sender, NetMQSocketEventArgs e)
        {
            Contract.Argument.Assert(e.IsReadyToReceive);
            var receivedMessage = _guardedResource.Update(() => _responseSocket.ReceiveMultipartMessage());

            var client = receivedMessage[0].ToByteArray();
            var messageId = new Guid(receivedMessage[1].ToByteArray());
            var messageTypeString = receivedMessage[2].ConvertToString();
            var messageBody = receivedMessage[3].ConvertToString();
            var messageType = messageTypeString.AsType();

            var message = (IMessage)JsonConvert.DeserializeObject(messageBody, messageType, JsonSettings.JsonSerializerSettings);

            Contract.State.Assert(messageId == message.MessageId);

            var task = Dispatch(message);

            if(message is IQuery || message.GetType().Implements(typeof(ICommand<>)))
            {
                task.ContinueWith(taskResult =>
                {
                    if(taskResult.IsFaulted)
                    {
                        _guardedResource.Update(() =>
                        {
                            _responseSocket.SendMoreFrame(client);
                            _responseSocket.SendMoreFrame(messageId.ToByteArray());
                            _responseSocket.SendFrame("FAIL");
                        });

                        return;
                    }

                    if(taskResult.IsCompleted)
                    {
                        var responseMessage = taskResult.Result;
                        if(responseMessage == null)
                        {
                            return;
                        }

                        _guardedResource.Update(() =>
                        {
                            _responseSocket.SendMoreFrame(client);
                            _responseSocket.SendMoreFrame(messageId.ToByteArray());
                            _responseSocket.SendMoreFrame("OK");
                            _responseSocket.SendMoreFrame(responseMessage.GetType().FullName);
                            _responseSocket.SendFrame(JsonConvert.SerializeObject(responseMessage, Formatting.Indented, JsonSettings.JsonSerializerSettings));
                        });
                    }
                });
            }

        }

        public void Stop()
        {
            Contract.Invariant.Assert(_running);
            _running = false;
            _cancellationTokenSource.Cancel();
            _poller.Dispose();
            _responseSocket.Close();
            _responseSocket.Dispose();
            _messagePumpThread.InterruptAndJoin();
        }

        void MessagePumpThread()
        {
            while(!_cancellationTokenSource.Token.IsCancellationRequested)
                try
                {
                    _globalStateTracker.AwaitDispatchableMessage(this, _dispatchingRules).Run();
                }
                catch(Exception exception) when(exception is OperationCanceledException || exception is ThreadInterruptedException)
                {
                    return;
                }
        }

        void EnqueueTransactionalTask(IMessage message, Action action)
            => EnqueueNonTransactionalTask(message, action: () => TransactionScopeCe.Execute(action));

        void EnqueueNonTransactionalTask(IMessage message, Action action)
            => _globalStateTracker.EnqueueMessageTask(this, message, messageTask: () => _serviceLocator.ExecuteInIsolatedScope(action));

        public Task<object> Dispatch(IMessage message)
        {
            switch(message)
            {
                case ICommand command:
                    return DispatchAsync(command);
                case IEvent @event:
                    return DispatchAsync(@event);
                case IQuery query:
                    return DispatchAsync(query);
                default:
                    throw new Exception($"Unsupported message type: {message.GetType()}");
            }
        }
        public string Address => _address;

        async Task<object> DispatchAsync(IQuery query)
        {
            var handler = _handlerRegistry.GetQueryHandler(query.GetType());

            var taskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            using(_guardedResource.AwaitUpdateLock())
            {
                EnqueueNonTransactionalTask(query,
                                            action: () =>
                                            {
                                                try
                                                {
                                                    var result = handler(query);
                                                    taskCompletionSource.SetResult(result);
                                                }
                                                catch(Exception exception)
                                                {
                                                    taskCompletionSource.SetException(exception);
                                                    throw;
                                                }
                                            });
            }
            return await taskCompletionSource.Task.NoMarshalling();
        }

        async Task<object> DispatchAsync(IEvent @event)
        {
            var handler = _handlerRegistry.GetEventHandlers(@event.GetType());
            var taskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            using(_guardedResource.AwaitUpdateLock())
            {
                EnqueueTransactionalTask(@event,
                                         action: () =>
                                         {
                                             try
                                             {
                                                 handler.ForEach(action: @this => @this(@event));
                                                 taskCompletionSource.SetResult(result: null);
                                             }
                                             catch(Exception exception)
                                             {
                                                 taskCompletionSource.SetException(exception);
                                                 throw;
                                             }
                                         });
            }
            return await taskCompletionSource.Task.NoMarshalling();
        }

        async Task<object> DispatchAsync(ICommand command)
        {
            var handler = _handlerRegistry.GetCommandHandler(command.GetType());

            var taskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            using(_guardedResource.AwaitUpdateLock())
            {
                EnqueueTransactionalTask(command,
                                         action: () =>
                                         {
                                             try
                                             {
                                                 var result = handler(command);
                                                 taskCompletionSource.SetResult(result);
                                             }
                                             catch(Exception exception)
                                             {
                                                 taskCompletionSource.SetException(exception);
                                                 throw;
                                             }
                                         });
            }
            return await taskCompletionSource.Task.NoMarshalling();
        }

        public void Dispose()
        {
            if(_running)
                Stop();
        }
    }
}
