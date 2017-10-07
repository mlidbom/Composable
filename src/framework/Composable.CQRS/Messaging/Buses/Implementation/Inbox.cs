using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.System;
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

        public Inbox(IServiceLocator serviceLocator, IInProcessServiceBus inProcessServiceBus, IGlobalBusStrateTracker globalStateTracker)
        {
            _serviceLocator = serviceLocator;
            _inProcessServiceBus = inProcessServiceBus;
            _globalStateTracker = globalStateTracker;
            _cancellationTokenSource = new CancellationTokenSource();
            _messagePumpThread = new Thread(MessagePumpThread)
            {
                Name = "_MessagePump",
                Priority = ThreadPriority.AboveNormal
            };
        }

        public void Start() => _guardedResource.Update(() =>
        {
            BetterContract.Assert.That(!_running);
            _running = true;
            _messagePumpThread.Start();
        });

        public void Stop() => _guardedResource.Update(() =>
        {
            BetterContract.Assert.That(_running);
            _running = false;
            _cancellationTokenSource.Cancel();
            _messagePumpThread.InterruptAndJoin();
        });

        public void Send(ICommand command) => _guardedResource.Update(() => EnqueueTransactionalTask(command, () => _inProcessServiceBus.Send(command)));

        public void Publish(IEvent anEvent) => _guardedResource.Update(() => EnqueueTransactionalTask(anEvent, () => _inProcessServiceBus.Publish(anEvent)));

        public async Task<TResult> SendAsync<TResult>(ICommand<TResult> command) where TResult : IMessage
        {
            var taskCompletionSource = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (_guardedResource.AwaitUpdateLock())
            {
                EnqueueTransactionalTask(command,
                                         () =>
                                         {
                                             try
                                             {
                                                 var result = _inProcessServiceBus.Send(command);
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

        public async Task<TResult> QueryAsync<TResult>(IQuery<TResult> query) where TResult : IQueryResult
        {
            var taskCompletionSource = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (_guardedResource.AwaitUpdateLock())
            {
                EnqueueNonTransactionalTask(query,
                                            () =>
                                            {
                                                try
                                                {
                                                    var result = _inProcessServiceBus.Get(query);
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

        public TResult Query<TResult>(IQuery<TResult> query) where TResult : IQueryResult
            => QueryAsync(query).Result;

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
                    return Dispatch(command);
                case IEvent @event:
                    return Dispatch(@event);
                case IQuery query:
                    return Dispatch(query);
                default:
                    throw new Exception($"Unsupported message type: {message.GetType()}");
            }
        }

        Task<object> Dispatch(IQuery query)
        {
            if(query.GetType().Implements(typeof(IQuery<>)))
            {
                
            }

            return Task.FromResult((object)null);
        }

        Task<object> Dispatch(IEvent @event)
        {
            return Task.FromResult((object)null);
        }

        Task<object> Dispatch(ICommand command)
        {
            return Task.FromResult((object)null);
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
