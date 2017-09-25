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

namespace Composable.Messaging.Buses
{
    partial class InterprocessServiceBus : IInterProcessServiceBus
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
                                                                                new QueriesExecuteAfterAllCommandsAndEventsAreDone()
                                                                            };
        bool _running;
        readonly Thread _messagePumpThread;

        const int DispatchThreadCount = 5;
        readonly List<Thread> _messageDispatchThread;

        public IReadOnlyList<Exception> ThrownExceptions => _thrownExceptions.ToList();

        public InterprocessServiceBus(string name, DummyTimeSource timeSource, IInProcessServiceBus inProcessServiceBus, IGlobalBusStrateTracker globalStateTracker)
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

            _messageDispatchThread = 1.Through(DispatchThreadCount).Select(index => new Thread(MessageDispatchThread)
                                                                                    {
                                                                                        Name = $"{_name}_MessageDispatchThread_{index}"
                                                                                    }).ToList();
        }

        public void Start()
        {
            using(_globalStateTracker.ResourceGuard.AwaitExclusiveLock())
            {
                Contract.Assert.That(!_running, "!_running");
                _running = true;
                _messagePumpThread.Start();
                _messageDispatchThread.ForEach(thread => thread.Start());
            }
        }

        public void Stop()
        {
            Contract.Assert.That(_running, "_running");
            _running = false;
            _cancellationTokenSource.Cancel();
            _messageDispatchThread.ForEach(thread => thread.Interrupt());
            _messagePumpThread.Interrupt();
            _messageDispatchThread.ForEach(thread => thread.Join());
            _messagePumpThread.Join();
        }

        public void AwaitNoMessagesInFlight() => _globalStateTracker.ResourceGuard.ExecuteWithResourceExclusivelyLockedWhen(condition: () => _queuedTasks.Count == 0, action: () => {});

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
                    catch (Exception exception) when (IsShuttingDownException(exception))
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
                catch(Exception exception) when (IsShuttingDownException(exception))
                {
                    return;
                }

                try
                {
                    dispatchingTask.DispatchMessageTask.RunSynchronously();
                    _globalStateTracker.ResourceGuard.ExecuteWithResourceExclusivelyLocked(() =>
                    {
                        _queuedTasks.Remove(dispatchingTask);
                        dispatchingTask.MessageDispatchingTracker.Succeeded();
                    });
                }
                catch(Exception exception) when (IsShuttingDownException(exception))
                {
                    return;
                }
                catch(Exception exception)
                {
                    _globalStateTracker.ResourceGuard.ExecuteWithResourceExclusivelyLocked(() =>
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
            dispatchingTask = _queuedTasks.Where(task => !task.IsDispatching).FirstOrDefault(task => CanBeDispatched(state, task));
            if(dispatchingTask != null)
            {
                return true;
            }

            return false;
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
            using(_globalStateTracker.ResourceGuard.AwaitExclusiveLock())
            {
                if(_timeSource.UtcNow > sendAt.ToUniversalTime())
                    throw new InvalidOperationException(message: "You cannot schedule a message to be sent in the past.");

                _scheduledMessages.Add(new ScheduledMessage(sendAt, message));
            }
        }

        public void Dispose() { _managedResources.Dispose(); }

        public void Publish(IEvent anEvent) =>
            _globalStateTracker.ResourceGuard.ExecuteWithResourceExclusivelyLocked(
                () =>
                {
                    var messageDispatchingTracker = _globalStateTracker.QueuedMessage(anEvent, null);
                    _queuedTasks.Add(new DispatchingTask(anEvent, messageDispatchingTracker, () => _inProcessServiceBus.Publish(anEvent)));
                });

        public void Send(ICommand command) =>
            _globalStateTracker.ResourceGuard.ExecuteWithResourceExclusivelyLocked(
                () =>
                {
                    var messageDispatchingTracker = _globalStateTracker.QueuedMessage(command, null);
                    _queuedTasks.Add(new DispatchingTask(command, messageDispatchingTracker, () => _inProcessServiceBus.Send(command)));
                });

        public TResult Query<TResult>(IQuery<TResult> query) where TResult : IQueryResult => QueryAsync(query).Result;

        public Task<TResult> QueryAsync<TResult>(IQuery<TResult> query) where TResult : IQueryResult
            => _globalStateTracker.ResourceGuard.ExecuteWithResourceExclusivelyLocked(
                () =>
                {
                    var messageDispatchingTracker = _globalStateTracker.QueuedMessage(query, null);
                    var dispatchMessageTask = new Task<TResult>(() => _inProcessServiceBus.Get(query));
                    _queuedTasks.Add(new DispatchingTask(query, messageDispatchingTracker, dispatchMessageTask));
                    return dispatchMessageTask;
                });

        public override string ToString() => _name;
    }
}
