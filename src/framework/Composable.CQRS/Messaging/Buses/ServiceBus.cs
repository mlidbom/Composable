using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.GenericAbstractions.Time;
using Composable.System;
using Composable.System.Linq;
using Composable.System.Reactive;
using Composable.System.Threading.ResourceAccess;
using Composable.System.Transactions;

namespace Composable.Messaging.Buses
{
    partial class ServiceBus : IServiceBus
    {
        readonly string _name;
        readonly DummyTimeSource _timeSource;
        readonly IInProcessServiceBus _inProcessServiceBus;
        readonly IGlobalBusStrateTracker _globalStateTracker;
        readonly List<ScheduledMessage> _scheduledMessages = new List<ScheduledMessage>();
        readonly List<DispatchingTask> _queuedTasks = new List<DispatchingTask>();

        readonly IDisposable _managedResources;
        readonly IList<Exception> _thrownExceptions = new List<Exception>();
        readonly CancellationTokenSource _cancellationTokenSource;

        readonly BlockingCollection<DispatchingTask> _dispatchingTasks = new BlockingCollection<DispatchingTask>();

        readonly IReadOnlyList<IMessageDispatchingRule> _dispatchingRules = new List<IMessageDispatchingRule>()
                                                                            {
                                                                                new QueriesExecuteAfterAllCommandsAndEventsAreDone(),
                                                                                new CommandsAndEventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint()
                                                                            };
        bool _running;
        readonly Thread _messagePumpThread;

        const int DispatchThreadCount = 5;
        readonly List<Thread> _messageDispatchThread;

        public IReadOnlyList<Exception> ThrownExceptions => _thrownExceptions.ToList();

        public ServiceBus(string name, DummyTimeSource timeSource, IInProcessServiceBus inProcessServiceBus, IGlobalBusStrateTracker globalStateTracker)
        {
            _name = name;
            _timeSource = timeSource;
            _cancellationTokenSource = new CancellationTokenSource();
            _inProcessServiceBus = inProcessServiceBus;
            _globalStateTracker = globalStateTracker;
            _managedResources = timeSource.UtcNowChanged.Subscribe(SendDueMessages);

            _messagePumpThread = new Thread(MessagePumpThread)
                                 {
                                     Name = $"{_name}_MessagePump"
                                 };

            _messageDispatchThread = 1.Through(DispatchThreadCount)
                                      .Select(selector: index => new Thread(MessageDispatchThread)
                                                       {
                                                           Name = $"{_name}_MessageDispatchThread_{index}"
                                                       }).ToList();
        }

        public void Start()
        {
            Contract.Assert.That(!_running, message: "!_running");
            _running = true;
            _messagePumpThread.Start();
            _messageDispatchThread.ForEach(action: thread => thread.Start());
        }

        public void Stop()
        {
            Contract.Assert.That(_running, message: "_running");
            _running = false;
            _cancellationTokenSource.Cancel();
            _messageDispatchThread.ForEach(action: thread => thread.Interrupt());
            _messagePumpThread.Interrupt();
            _messageDispatchThread.ForEach(action: thread => thread.Join());
            _messagePumpThread.Join();
        }

        public void SendAtTime(DateTime sendAt, ICommand message)
        {
            using(_globalStateTracker.ResourceGuard.AwaitExclusiveLock())
            {
                if(_timeSource.UtcNow > sendAt.ToUniversalTime())
                    throw new InvalidOperationException(message: "You cannot schedule a message to be sent in the past.");

                _scheduledMessages.Add(new ScheduledMessage(sendAt, message));
            }
        }

        public void Send(ICommand command) =>
            _globalStateTracker.ResourceGuard.ExecuteWithResourceExclusivelyLocked(
                action: () =>
                {
                    var messageDispatchingTracker = _globalStateTracker.QueuedMessage(command, triggeringMessage: null);
                    _queuedTasks.Add(new DispatchingTask(command, messageDispatchingTracker, dispatchMessageTask: () => _inProcessServiceBus.Send(command)));
                });

        public void Publish(IEvent anEvent) =>
            _globalStateTracker.ResourceGuard.ExecuteWithResourceExclusivelyLocked(
                action: () =>
                {
                    var messageDispatchingTracker = _globalStateTracker.QueuedMessage(anEvent, triggeringMessage: null);
                    _queuedTasks.Add(new DispatchingTask(anEvent, messageDispatchingTracker, dispatchMessageTask: () => _inProcessServiceBus.Publish(anEvent)));
                });

        public Task<TResult> QueryAsync<TResult>(IQuery<TResult> query) where TResult : IQueryResult
            => _globalStateTracker.ResourceGuard.ExecuteWithResourceExclusivelyLocked(
                function: () =>
                {
                    var messageDispatchingTracker = _globalStateTracker.QueuedMessage(query, triggeringMessage: null);
                    var dispatchMessageTask = new Task<TResult>(function: () => _inProcessServiceBus.Get(query));
                    _queuedTasks.Add(new DispatchingTask(query, messageDispatchingTracker, dispatchMessageTask));
                    return dispatchMessageTask;
                });

        public TResult Query<TResult>(IQuery<TResult> query) where TResult : IQueryResult => QueryAsync(query).Result;

        void MessagePumpThread()
        {
            using(var globalStateLock = _globalStateTracker.ResourceGuard.AwaitExclusiveLock())
            {
                while(!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    while(TryGetDispatchableMessages(out var dispatchingTask))
                    {
                        dispatchingTask.IsDispatching = true;
                        _dispatchingTasks.Add(dispatchingTask);
                    }

                    try
                    {
                        globalStateLock.ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(7.Days());
                    }
                    catch(Exception exception) when(IsShuttingDownException(exception))
                    {
                        return;
                    }
                }
            }
        }

        void MessageDispatchThread()
        {
            while(!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                DispatchingTask dispatchingTask;
                try
                {
                    dispatchingTask = _dispatchingTasks.Take(_cancellationTokenSource.Token);
                }
                catch(Exception exception) when(IsShuttingDownException(exception))
                {
                    return;
                }

                try
                {
                    switch(dispatchingTask.Message)
                    {
                        case ICommand _:
                        case IEvent _:
                            TransactionScopeCe.Execute(action: () => dispatchingTask.DispatchMessageTask.RunSynchronously());
                            break;
                        case IQuery _:
                            dispatchingTask.DispatchMessageTask.RunSynchronously();
                            break;
                        default: throw new Exception($"Unknown message type {dispatchingTask.Message.GetType().AssemblyQualifiedName}");
                    }

                    _globalStateTracker.ResourceGuard.ExecuteWithResourceExclusivelyLocked(action: () =>
                    {
                        _queuedTasks.Remove(dispatchingTask);
                        dispatchingTask.MessageDispatchingTracker.Succeeded();
                    });
                }
                catch(Exception exception) when(IsShuttingDownException(exception))
                {
                    return;
                }
                catch(Exception exception)
                {
                    _globalStateTracker.ResourceGuard.ExecuteWithResourceExclusivelyLocked(action: () =>
                    {
                        _queuedTasks.Remove(dispatchingTask);
                        dispatchingTask.MessageDispatchingTracker.Failed();
                        _thrownExceptions.Add(exception);
                    });
                }
            }
        }

        static bool IsShuttingDownException(Exception exception) => exception is OperationCanceledException || exception is ThreadInterruptedException;

        bool TryGetDispatchableMessages(out DispatchingTask dispatchingTask)
        {
            var state = _globalStateTracker.CreateSnapshot();
            var locallyExecutingMessages = _queuedTasks.Where(predicate: task => task.IsDispatching).Select(selector: task => task.Message).ToList();
            dispatchingTask = _queuedTasks.Where(predicate: task => !task.IsDispatching).FirstOrDefault(predicate: task => CanBeDispatched(state, locallyExecutingMessages, task));
            if(dispatchingTask != null)
                return true;

            return false;
        }

        bool CanBeDispatched(IGlobalBusStateSnapshot state, IReadOnlyList<IMessage> locallyExecutingMessages, DispatchingTask task) => _dispatchingRules.All(predicate: rule => rule.CanBeDispatched(state, locallyExecutingMessages, task.Message));

        void SendDueMessages(DateTime currentTime)
        {
            var dueMessages = _scheduledMessages.Where(predicate: message => message.SendAt <= currentTime)
                                                .ToList();
            dueMessages.ForEach(action: scheduledMessage => _inProcessServiceBus.Send(scheduledMessage.Message));
            dueMessages.ForEach(action: message => _scheduledMessages.Remove(message));
        }

        public override string ToString() => _name;

        public void Dispose() { _managedResources.Dispose(); }
    }
}
