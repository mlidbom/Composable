using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Composable.GenericAbstractions.Time;
using Composable.System;
using Composable.System.Reactive;
using Composable.System.Threading.ResourceAccess;

namespace Composable.Messaging.Buses
{
    partial class InterprocessServiceBus : IInterProcessServiceBus
    {
        readonly DummyTimeSource _timeSource;
        readonly IInProcessServiceBus _inProcessServiceBus;
        readonly IGlobalBusStrateTracker _globalStateTracker;
        readonly List<ScheduledMessage> _scheduledMessages = new List<ScheduledMessage>();
        readonly List<DispatchingTask> _queuedTasks = new List<DispatchingTask>();

        readonly IDisposable _managedResources;
        readonly IExclusiveResourceAccessGuard _resourceGuard;
        readonly IList<Exception> _thrownExceptions = new List<Exception>();
        readonly CancellationTokenSource _cancellationTokenSource;

        readonly BlockingCollection<DispatchingTask> _dispatchingTasks = new BlockingCollection<DispatchingTask>();

        readonly IReadOnlyList<IMessageDispatchingRule> _dispatchingRules = new List<IMessageDispatchingRule>()
                                                                            {
                                                                                new QueriesExecuteAfterAllCommandsAndEventsAreDone()
                                                                            };

        public IReadOnlyList<Exception> ThrownExceptions => _thrownExceptions.ToList();

        public InterprocessServiceBus(DummyTimeSource timeSource, IInProcessServiceBus inProcessServiceBus, IGlobalBusStrateTracker globalStateTracker)
        {
            _timeSource = timeSource;
            _cancellationTokenSource = new CancellationTokenSource();
            _inProcessServiceBus = inProcessServiceBus;
            _globalStateTracker = globalStateTracker;
            _managedResources = timeSource.UtcNowChanged.Subscribe(SendDueMessages);
            _resourceGuard = ResourceAccessGuard.ExclusiveWithTimeout(30.Seconds());
            Start();
        }

        public void Start()
        {
            Task.Factory.StartNew(MessagePumpThread_, TaskCreationOptions.LongRunning);
            Task.Factory.StartNew(MessageDispatchThread_, TaskCreationOptions.LongRunning);
        }

        public void Stop() => _cancellationTokenSource.Cancel();
        public void AwaitNoMessagesInFlight() => _resourceGuard.ExecuteWithResourceExclusivelyLockedWhen(condition: () => _queuedTasks.Count == 0, action: () => {});

        void MessagePumpThread_()
        {
            using(var globalStateLock = _globalStateTracker.ResourceGuard.AwaitExclusiveLock())
            {
                while(!_cancellationTokenSource.IsCancellationRequested)
                {
                    using(_resourceGuard.AwaitExclusiveLock())
                    {
                        while(TryGetDispatchableMessages(out var dispatchingTask))
                        {
                            dispatchingTask.IsDispatching = true;
                            _dispatchingTasks.Add(dispatchingTask);
                        }
                    }

                    globalStateLock.ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(7.Days());
                }
            }
        }

        void MessageDispatchThread_()
        {
            while(!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                var dispatchingTask = _dispatchingTasks.Take(_cancellationTokenSource.Token);
                try
                {
                    dispatchingTask.DispatchMessageTask.RunSynchronously();
                    _resourceGuard.ExecuteWithResourceExclusivelyLockedAndNotifyWaitingThreadsAboutUpdate(() =>
                    {
                        _queuedTasks.Remove(dispatchingTask);
                        dispatchingTask.MessageDispatchingTracker.Succeeded();
                    });
                }
                catch(Exception exception)
                {
                    _resourceGuard.ExecuteWithResourceExclusivelyLockedAndNotifyWaitingThreadsAboutUpdate(() =>
                    {
                        _queuedTasks.Remove(dispatchingTask);
                        dispatchingTask.MessageDispatchingTracker.Failed();
                        _thrownExceptions.Add(exception);
                    });
                }
            }
        }

        bool TryGetDispatchableMessages(out DispatchingTask dispatchingTask)
        {
            var state = _globalStateTracker.CreateSnapshot();
            dispatchingTask = _queuedTasks.Where(task => !task.IsDispatching).FirstOrDefault(task => CanBeDispatched(state, task));
            return dispatchingTask != null;
        }

        bool CanBeDispatched(IGlobalBusStateSnapshot state, DispatchingTask task) => _dispatchingRules.All(rule => rule.CanBeDispatched(state, task.Message));

        void SendDueMessages(DateTime currentTime)
        {
            var dueMessages = _scheduledMessages.Where(predicate: message => message.SendAt <= currentTime)
                                                .ToList();
            dueMessages.ForEach(action: scheduledMessage => _inProcessServiceBus.Send(scheduledMessage.Message));
            dueMessages.ForEach(action: message => _scheduledMessages.Remove(message));
        }

        public void SendAtTime(DateTime sendAt, ICommand message)
        {
            using(_resourceGuard.AwaitExclusiveLock())
            {
                if(_timeSource.UtcNow > sendAt.ToUniversalTime())
                    throw new InvalidOperationException(message: "You cannot schedule a message to be sent in the past.");

                _scheduledMessages.Add(new ScheduledMessage(sendAt, message));
            }
        }

        public void Dispose() { _managedResources.Dispose(); }

        public void Publish(IEvent anEvent) =>
            _resourceGuard.ExecuteWithResourceExclusivelyLockedAndNotifyWaitingThreadsAboutUpdate(
                () =>
                {
                    var messageDispatchingTracker = _globalStateTracker.QueuedMessage(anEvent, null);
                    _queuedTasks.Add(new DispatchingTask(anEvent, messageDispatchingTracker, () => _inProcessServiceBus.Publish(anEvent)));
                });

        public void Send(ICommand command) =>
            _resourceGuard.ExecuteWithResourceExclusivelyLockedAndNotifyWaitingThreadsAboutUpdate(
                () =>
                {
                    var messageDispatchingTracker = _globalStateTracker.QueuedMessage(command, null);
                    _queuedTasks.Add(new DispatchingTask(command, messageDispatchingTracker, () => _inProcessServiceBus.Send(command)));
                });

        public TResult Query<TResult>(IQuery<TResult> query) where TResult : IQueryResult => QueryAsync(query).Result;

        public Task<TResult> QueryAsync<TResult>(IQuery<TResult> query) where TResult : IQueryResult
            => _resourceGuard.ExecuteWithResourceExclusivelyLockedAndNotifyWaitingThreadsAboutUpdate(
                () =>
                {
                    var messageDispatchingTracker = _globalStateTracker.QueuedMessage(query, null);
                    var dispatchMessageTask = new Task<TResult>(() => _inProcessServiceBus.Get(query));
                    _queuedTasks.Add(new DispatchingTask(query, messageDispatchingTracker, dispatchMessageTask));
                    return dispatchMessageTask;
                });
    }
}
