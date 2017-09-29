using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Composable.Contracts;
using Composable.GenericAbstractions.Time;
using Composable.System;
using Composable.System.Linq;
using Composable.System.Threading.ResourceAccess;

namespace Composable.Messaging.Buses
{
    partial class ServiceBus : IServiceBus
    {
        readonly string _name;
        readonly IUtcTimeTimeSource _timeSource;
        readonly IInProcessServiceBus _inProcessServiceBus;
        readonly IGlobalBusStrateTracker _globalStateTracker;
        readonly List<ScheduledCommand> _scheduledMessages = new List<ScheduledCommand>();
        readonly List<DispatchingTask> _queuedTasks = new List<DispatchingTask>();

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
        readonly Timer _scheduledMessagesTimer;

        public IReadOnlyList<Exception> ThrownExceptions => _thrownExceptions.ToList();

        public ServiceBus(string name, IUtcTimeTimeSource timeSource, IInProcessServiceBus inProcessServiceBus, IGlobalBusStrateTracker globalStateTracker)
        {
            _name = name;
            _timeSource = timeSource;
            _cancellationTokenSource = new CancellationTokenSource();
            _inProcessServiceBus = inProcessServiceBus;
            _globalStateTracker = globalStateTracker;

            _messagePumpThread = new Thread(MessagePumpThread)
                                 {
                                     Name = $"{_name}_MessagePump"
                                 };

            _messageDispatchThread = 1.Through(DispatchThreadCount)
                                      .Select(selector: index => new Thread(MessageDispatchThread)
                                                                 {
                                                                     Name = $"{_name}_MessageDispatchThread_{index}"
                                                                 }).ToList();

            _scheduledMessagesTimer = new Timer(_ => SendDueMessages(), null, 0.Seconds(), 100.Milliseconds());
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
            _scheduledMessagesTimer.Dispose();
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

                _scheduledMessages.Add(new ScheduledCommand(sendAt, message));
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

        public async Task<TResult> SendAsync<TResult>(ICommand<TResult> command) where TResult : IMessage
        {
            var taskCompletionSource = new TaskCompletionSource<TResult>();
            _globalStateTracker.ResourceGuard.ExecuteWithResourceExclusivelyLocked(
                () =>
                {
                    var messageDispatchingTracker = _globalStateTracker.QueuedMessage(command, triggeringMessage: null);
                    TResult result = default;
                    _queuedTasks.Add(new DispatchingTask(command, messageDispatchingTracker, () => result = _inProcessServiceBus.Send(command), () => taskCompletionSource.SetResult(result)));
                });

            return await taskCompletionSource.Task.ConfigureAwait(false);
        }

        public async Task<TResult> QueryAsync<TResult>(IQuery<TResult> query) where TResult : IQueryResult
        {
            TaskCompletionSource<TResult> taskCompletionSource = new TaskCompletionSource<TResult>();
            _globalStateTracker.ResourceGuard.ExecuteWithResourceExclusivelyLocked(
                () =>
                {
                    var messageDispatchingTracker = _globalStateTracker.QueuedMessage(query, triggeringMessage: null);
                    TResult result = default;
                    _queuedTasks.Add(new DispatchingTask(query, messageDispatchingTracker, () => result = _inProcessServiceBus.Get(query), () => taskCompletionSource.SetResult(result)));
                });

            return await taskCompletionSource.Task.ConfigureAwait(false);
        }

        public TResult Query<TResult>(IQuery<TResult> query) where TResult : IQueryResult => QueryAsync(query).Result;

        static bool IsShuttingDownException(Exception exception) => exception is OperationCanceledException || exception is ThreadInterruptedException;

        void SendDueMessages()
            => _globalStateTracker.ResourceGuard.ExecuteWithResourceExclusivelyLocked(() =>
            {
                var dueMessages = _scheduledMessages.Where(predicate: message => message.SendAt <= _timeSource.UtcNow)
                                                    .ToList();
                dueMessages.ForEach(action: scheduledCommand => Send(scheduledCommand.Command));
                dueMessages.ForEach(action: message => _scheduledMessages.Remove(message));
            });

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
