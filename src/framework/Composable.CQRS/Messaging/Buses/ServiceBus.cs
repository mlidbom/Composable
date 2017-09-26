using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.GenericAbstractions.Time;
using Composable.System.Linq;
using Composable.System.Reactive;
using Composable.System.Threading.ResourceAccess;

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
                () =>
                {
                    var messageDispatchingTracker = _globalStateTracker.QueuedMessage(command, triggeringMessage: null);
                    _queuedTasks.Add(new DispatchingTask(command, messageDispatchingTracker, dispatchMessageTask: () => _inProcessServiceBus.Send(command)));
                });

        public void Publish(IEvent anEvent) =>
            _globalStateTracker.ResourceGuard.ExecuteWithResourceExclusivelyLocked(
                () =>
                {
                    var messageDispatchingTracker = _globalStateTracker.QueuedMessage(anEvent, triggeringMessage: null);
                    _queuedTasks.Add(new DispatchingTask(anEvent, messageDispatchingTracker, dispatchMessageTask: () => _inProcessServiceBus.Publish(anEvent)));
                });

        public Task<TResult> QueryAsync<TResult>(IQuery<TResult> query) where TResult : IQueryResult
            => _globalStateTracker.ResourceGuard.ExecuteWithResourceExclusivelyLocked(
                () =>
                {
                    var messageDispatchingTracker = _globalStateTracker.QueuedMessage(query, triggeringMessage: null);
                    var dispatchMessageTask = new Task<TResult>(function: () => _inProcessServiceBus.Get(query));
                    _queuedTasks.Add(new DispatchingTask(query, messageDispatchingTracker, dispatchMessageTask));
                    return dispatchMessageTask;
                });

        public TResult Query<TResult>(IQuery<TResult> query) where TResult : IQueryResult => QueryAsync(query).Result;

        static bool IsShuttingDownException(Exception exception) => exception is OperationCanceledException || exception is ThreadInterruptedException;

        void SendDueMessages(DateTime currentTime)
        {
                var dueMessages = _scheduledMessages.Where(predicate: message => message.SendAt <= currentTime)
                                                    .ToList();
            dueMessages.ForEach(action: scheduledMessage => _inProcessServiceBus.Send(scheduledMessage.Message));
                dueMessages.ForEach(action: message => _scheduledMessages.Remove(message));
        }

        public override string ToString() => _name;

        public void Dispose()
        {
            if(_running)
            {
                Stop();
            }
            _managedResources.Dispose();
        }
    }
}
