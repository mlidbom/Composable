using System;
using System.Collections.Generic;
using System.Linq;
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


        public IReadOnlyList<Exception> ThrownExceptions => _thrownExceptions.ToList();

        public TestingOnlyInterprocessServiceBus(DummyTimeSource timeSource, IInProcessServiceBus inProcessServiceBus)
        {
            _timeSource = timeSource;
            _inProcessServiceBus = inProcessServiceBus;
            _managedResources = timeSource.UtcNowChanged.Subscribe(SendDueMessages);
            _resourceGuard = ResourceAccessGuard.ExclusiveWithTimeout(30.Seconds());
            Start();
        }

        internal void Start() { Task.Factory.StartNew(MessagePumpThread_, TaskCreationOptions.LongRunning); }

        void MessagePumpThread_()
        {
            using(var exclusiveAccess = _resourceGuard.AwaitExclusiveLock())
            {
                while(true)
                {
                    exclusiveAccess.ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(TimeSpan.FromMilliseconds(int.MaxValue));
                    if(_dispatchingTasks.Count > 0)
                    {
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
            }
        }

        void SendDueMessages(DateTime currentTime)
        {
            var dueMessages = _scheduledMessages.Where(message => message.SendAt <= currentTime)
                                                .ToList();
            dueMessages.ForEach(scheduledMessage => _inProcessServiceBus.Send(scheduledMessage.Message));
            dueMessages.ForEach(message => _scheduledMessages.Remove(message));
        }

        public void SendAtTime(DateTime sendAt, ICommand message)
        {
            using(_resourceGuard.AwaitExclusiveLock())
            {
                if(_timeSource.UtcNow > sendAt.ToUniversalTime())
                {
                    throw new InvalidOperationException("You cannot schedule a message to be sent in the past.");
                }

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

        public void Dispose()
        {
            _managedResources.Dispose();
        }

        public void Publish(IEvent anEvent) =>
            _resourceGuard.ExecuteWithResourceExclusivelyLockedAndNotifyWaitingThreadsAboutUpdate(
                () => _dispatchingTasks.Enqueue(new Task(() => _inProcessServiceBus.Publish(anEvent))));

        public void Send(ICommand command) =>
            _resourceGuard.ExecuteWithResourceExclusivelyLockedAndNotifyWaitingThreadsAboutUpdate(
                () => _dispatchingTasks.Enqueue(new Task(() => _inProcessServiceBus.Send(command))));

        public TResult Query<TResult>(IQuery<TResult> query) where TResult : IQueryResult
            => _resourceGuard.ExecuteWithResourceExclusivelyLockedWhen(
                condition: () => _dispatchingTasks.Count == 0,
                function: () => _inProcessServiceBus.Get(query));


        public Task<TResult> QueryAsync<TResult>(IQuery<TResult> query) where TResult : IQueryResult
        {
            return _resourceGuard.ExecuteWithResourceExclusivelyLockedAndNotifyWaitingThreadsAboutUpdate(
                () =>
                {
                    var queryTask = new Task<TResult>(() => _inProcessServiceBus.Get(query));
                    _dispatchingTasks.Enqueue(queryTask);
                    return queryTask;
                });
        }
    }
}
