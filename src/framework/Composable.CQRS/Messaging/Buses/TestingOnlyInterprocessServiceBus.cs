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
    class TestingOnlyInterprocessServiceBus : IInterProcessServiceBus, IDisposable
    {
        readonly DummyTimeSource _timeSource;
        readonly IInProcessServiceBus _inProcessServiceBus;
        readonly List<ScheduledMessage> _scheduledMessages = new List<ScheduledMessage>();
        readonly Queue<Task> _dispatchingTasks = new Queue<Task>();
        readonly IDisposable _managedResources;
        readonly IExclusiveResourceAccessGuard _resourceGuard;
        readonly IList<Exception> _thrownExceptions = new List<Exception>();
        readonly CancellationTokenSource _cancellationTokenSource;

        public IReadOnlyList<Exception> ThrownExceptions => _thrownExceptions.ToList();

        public TestingOnlyInterprocessServiceBus(DummyTimeSource timeSource, IInProcessServiceBus inProcessServiceBus)
        {
            _timeSource = timeSource;
            _cancellationTokenSource = new CancellationTokenSource();
            _inProcessServiceBus = inProcessServiceBus;
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
                    exclusiveAccess.ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(7.Days());
                    if(_dispatchingTasks.Count > 0)
                        try
                        {
                            _dispatchingTasks.Dequeue().RunSynchronously();
                        }
                        catch(Exception exception)
                        {
                            _thrownExceptions.Add(exception);
                        }
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }

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

        class ScheduledMessage
        {
            public DateTime SendAt { get; }
            public ICommand Message { get; }

            public ScheduledMessage(DateTime sendAt, ICommand message)
            {
                SendAt = sendAt.SafeToUniversalTime();
                Message = message;
            }
        }

        public void Dispose() { _managedResources.Dispose(); }

        public void Publish(IEvent anEvent) =>
            _resourceGuard.ExecuteWithResourceExclusivelyLockedAndNotifyWaitingThreadsAboutUpdate(
                action: () => _dispatchingTasks.Enqueue(new Task(action: () => _inProcessServiceBus.Publish(anEvent))));

        public void Send(ICommand command) =>
            _resourceGuard.ExecuteWithResourceExclusivelyLockedAndNotifyWaitingThreadsAboutUpdate(
                action: () => _dispatchingTasks.Enqueue(new Task(action: () => _inProcessServiceBus.Send(command))));

        public TResult Query<TResult>(IQuery<TResult> query) where TResult : IQueryResult
            => _resourceGuard.ExecuteWithResourceExclusivelyLockedWhen(
                condition: () => _dispatchingTasks.Count == 0,
                function: () => _inProcessServiceBus.Get(query));

        public Task<TResult> QueryAsync<TResult>(IQuery<TResult> query) where TResult : IQueryResult
            => Task.Run(() => Query(query));
    }
}
