using System;
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
        readonly List<DispatchingTask> _dispatchingTasks = new List<DispatchingTask>();

        readonly IDisposable _managedResources;
        readonly IExclusiveResourceAccessGuard _resourceGuard;
        readonly IList<Exception> _thrownExceptions = new List<Exception>();
        readonly CancellationTokenSource _cancellationTokenSource;

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

        public void Start() => Task.Factory.StartNew(MessagePumpThread_, TaskCreationOptions.LongRunning);
        public void Stop() => _cancellationTokenSource.Cancel();
        public void AwaitNoMessagesInFlight() => _resourceGuard.ExecuteWithResourceExclusivelyLockedWhen(condition: () => _dispatchingTasks.Count == 0, action: () => {});

        void MessagePumpThread_()
        {
            using(var exclusiveAccess = _resourceGuard.AwaitExclusiveLock())
            {
                while(!_cancellationTokenSource.IsCancellationRequested)
                {
                    var state = new BusStateSnapshot(this);
                    DispatchingTask dispatchingTask;
                    while(null != (dispatchingTask = _dispatchingTasks.FirstOrDefault(task => CanBeDispatched(state, task))))
                    {
                        try
                        {
                            dispatchingTask.DispatchMessageTask.RunSynchronously();
                            _dispatchingTasks.Remove(dispatchingTask);
                        }
                        catch(Exception exception)
                        {
                            _thrownExceptions.Add(exception);
                        }
                    }
                    exclusiveAccess.ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(7.Days());
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }

        bool CanBeDispatched(BusStateSnapshot state, DispatchingTask task) => _dispatchingRules.All(rule => rule.CanBeDispatched(state, task.Message));

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
                action: () =>
                {
                    _globalStateTracker.QueuedMessage(anEvent, null);
                    _dispatchingTasks.Add(new DispatchingTask(anEvent, () => _inProcessServiceBus.Publish(anEvent)));
                });

        public void Send(ICommand command) =>
            _resourceGuard.ExecuteWithResourceExclusivelyLockedAndNotifyWaitingThreadsAboutUpdate(
                action: () =>
                {
                    _globalStateTracker.QueuedMessage(command, null);
                    _dispatchingTasks.Add(new DispatchingTask(command, () => _inProcessServiceBus.Send(command)));
                });

        public TResult Query<TResult>(IQuery<TResult> query) where TResult : IQueryResult
            => _resourceGuard.ExecuteWithResourceExclusivelyLockedWhen(
                condition: () => _dispatchingTasks.Count == 0,
                function: () => _inProcessServiceBus.Get(query));

        public Task<TResult> QueryAsync<TResult>(IQuery<TResult> query) where TResult : IQueryResult
            => Task.Run(() => Query(query));
    }
}
