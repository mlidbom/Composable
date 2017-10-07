using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.System;
using Composable.System.Linq;
using Composable.System.Reflection;
using Composable.System.Threading;
using Composable.System.Threading.ResourceAccess;
using Composable.System.Transactions;

namespace Composable.Messaging.Buses.Implementation
{
    class Inbox : IInbox, IDisposable
    {
        readonly IServiceLocator _serviceLocator;
        readonly IInProcessServiceBus _inProcessServiceBus;
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

        public IReadOnlyList<Exception> ThrownExceptions => _globalStateTracker.GetExceptionsFor(this);

        public Inbox(IServiceLocator serviceLocator, IInProcessServiceBus inProcessServiceBus, IGlobalBusStrateTracker globalStateTracker, IMessageHandlerRegistry handlerRegistry)
        {
            _serviceLocator = serviceLocator;
            _inProcessServiceBus = inProcessServiceBus;
            _globalStateTracker = globalStateTracker;
            _handlerRegistry = handlerRegistry;
            _cancellationTokenSource = new CancellationTokenSource();
            _messagePumpThread = new Thread(MessagePumpThread)
            {
                Name = "_MessagePump",
                Priority = ThreadPriority.AboveNormal
            };
        }

        public void Start() => _guardedResource.Update(() =>
        {
            Contract.Invariant.Assert(!_running);
            _running = true;
            _messagePumpThread.Start();
        });

        public void Stop() => _guardedResource.Update(() =>
        {
            Contract.Invariant.Assert(_running);
            _running = false;
            _cancellationTokenSource.Cancel();
            _messagePumpThread.InterruptAndJoin();
        });

        void MessagePumpThread()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    _globalStateTracker.AwaitDispatchableMessage(this, _dispatchingRules).Run();
                }
                catch (Exception exception) when (exception is OperationCanceledException || exception is ThreadInterruptedException)
                {
                    return;
                }
            }
        }

        void EnqueueTransactionalTask(IMessage message, Action action)
            => EnqueueNonTransactionalTask(message, () => TransactionScopeCe.Execute(action));

        void EnqueueNonTransactionalTask(IMessage message, Action action)
            => _globalStateTracker.EnqueueMessageTask(this, message, messageTask: () => _serviceLocator.ExecuteInIsolatedScope(action));

        public Task<object> Dispatch(IMessage message)
        {
            switch (message)
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

        async Task<object> DispatchAsync(IQuery query)
        {
            var handler = _handlerRegistry.GetQueryHandler(query.GetType());

            var taskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (_guardedResource.AwaitUpdateLock())
            {
                EnqueueNonTransactionalTask(query,
                                            () =>
                                            {
                                                try
                                                {
                                                    var result = handler(query);
                                                    taskCompletionSource.SetResult(result);
                                                }
                                                catch (Exception exception)
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
            using (_guardedResource.AwaitUpdateLock())
            {
                EnqueueTransactionalTask(@event,
                                         () =>
                                         {
                                             try
                                             {
                                                 handler.ForEach(@this => @this(@event));
                                                 taskCompletionSource.SetResult(null);
                                             }
                                             catch (Exception exception)
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
            using (_guardedResource.AwaitUpdateLock())
            {
                EnqueueTransactionalTask(command,
                                         () =>
                                         {
                                             try
                                             {
                                                 var result = handler(command);
                                                 taskCompletionSource.SetResult(result);
                                             }
                                             catch (Exception exception)
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
            if (_running)
            {
                Stop();
            }
        }
    }
}
