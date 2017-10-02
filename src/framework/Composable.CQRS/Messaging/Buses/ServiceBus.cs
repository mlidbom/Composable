using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.GenericAbstractions.Time;
using Composable.System;
using Composable.System.Threading;
using Composable.System.Threading.ResourceAccess;
using Composable.System.Transactions;

namespace Composable.Messaging.Buses
{
    partial class ServiceBus : IServiceBus
    {
        readonly string _name;
        readonly IInProcessServiceBus _inProcessServiceBus;
        readonly IGlobalBusStrateTracker _globalStateTracker;
        readonly CommandScheduler _commandScheduler;

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

        public ServiceBus(string name, IUtcTimeTimeSource timeSource, IInProcessServiceBus inProcessServiceBus, IGlobalBusStrateTracker globalStateTracker)
        {
            _name = name;
            _inProcessServiceBus = inProcessServiceBus;
            _globalStateTracker = globalStateTracker;
            _commandScheduler = new CommandScheduler(this, timeSource);
            _cancellationTokenSource = new CancellationTokenSource();
            _messagePumpThread = new Thread(MessagePumpThread)
                                 {
                                     Name = $"{_name}_MessagePump",
                                     Priority = ThreadPriority.AboveNormal
                                 };
        }

        public void Start() => _guardedResource.Update(() =>
        {
            Contract.Assert.That(!_running, message: "!_running");
            _running = true;
            _commandScheduler.Start();
            _messagePumpThread.Start();
        });

        public void Stop() => _guardedResource.Update(() =>
        {
            Contract.Assert.That(_running, message: "_running");
            _running = false;
            _cancellationTokenSource.Cancel();
            _commandScheduler.Dispose();
            _messagePumpThread.InterruptAndJoin();
        });

        public void SendAtTime(DateTime sendAt, ICommand command) => _commandScheduler.Schedule(sendAt, command);

        public void Send(ICommand command) => _guardedResource.Update(() => EnqueueTransactionalTask(command, () => _inProcessServiceBus.Send(command)));

        public void Publish(IEvent anEvent) => _guardedResource.Update(() => EnqueueTransactionalTask(anEvent, () => _inProcessServiceBus.Publish(anEvent)));

        public async Task<TResult> SendAsync<TResult>(ICommand<TResult> command) where TResult : IMessage
        {
            var taskCompletionSource = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            using(_guardedResource.AwaitExclusiveLock())
            {
                EnqueueTransactionalTask(command,
                                         () =>
                                         {
                                             try
                                             {
                                                 var result = _inProcessServiceBus.Send(command);
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

        public async Task<TResult> QueryAsync<TResult>(IQuery<TResult> query) where TResult : IQueryResult
        {
            var taskCompletionSource = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            using(_guardedResource.AwaitExclusiveLock())
            {
                EnqueueNonTransactionalTask(query,
                                            () =>
                                            {
                                                try
                                                {
                                                    var result = _inProcessServiceBus.Get(query);
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

        public TResult Query<TResult>(IQuery<TResult> query) where TResult : IQueryResult
            => QueryAsync(query).Result;

        void MessagePumpThread()
        {
            while(!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    _globalStateTracker.AwaitDispatchableMessage(this, _dispatchingRules).Run();
                }
                catch(Exception exception) when(exception is OperationCanceledException || exception is ThreadInterruptedException)
                {
                    return;
                }
            }
        }

        void EnqueueTransactionalTask(IMessage message, Action action)
            => EnqueueNonTransactionalTask(message, () => TransactionScopeCe.Execute(action));

        void EnqueueNonTransactionalTask(IMessage message, Action action)
            => _globalStateTracker.EnqueueMessageTask(this, message, messageTask: action);

        public override string ToString() => _name;

        public void Dispose()
        {
            if(_running)
            {
                Stop();
            }
        }
    }
}
