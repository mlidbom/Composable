using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Composable.System;
using Composable.System.Threading.ResourceAccess;

namespace Composable.Messaging.Buses
{
    partial class ServiceBus
    {
        void MessagePumpThread()
        {
            using(var globalStateLock = _globalStateTracker.ResourceGuard.AwaitExclusiveLock())
            {
                while(!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    while(TryGetDispatchableMessage(out var task))
                    {
                        var dispatchingTask = task;
                        dispatchingTask.IsDispatching = true;
                        Task.Run(() => DispatchTask(dispatchingTask));
                    }

                    try
                    {
                        globalStateLock.ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(7.Days());
                    }
                    catch(Exception exception) when(exception is OperationCanceledException || exception is ThreadInterruptedException)
                    {
                        return;
                    }
                }
            }
        }

        void DispatchTask(DispatchingTask task)
        {
            try
            {
                task.DispatchMessageTask();

                _globalStateTracker.ResourceGuard.ExecuteWithResourceExclusivelyLocked(action: () =>
                {
                    _queuedTasks.Remove(task);
                    task.MessageDispatchingTracker.Succeeded();
                });
            }
            catch(Exception exception)
            {
                _globalStateTracker.ResourceGuard.ExecuteWithResourceExclusivelyLocked(action: () =>
                {
                    _queuedTasks.Remove(task);
                    task.MessageDispatchingTracker.Failed();
                    _thrownExceptions.Add(exception);
                });
            }
        }

        bool TryGetDispatchableMessage(out DispatchingTask dispatchingTask)
        {
            var state = _globalStateTracker.CreateSnapshot();

            var locallyExecutingMessages = _queuedTasks.Where(queuedTask => queuedTask.IsDispatching).Select(queuedTask => queuedTask.Message).ToList();

            dispatchingTask = _queuedTasks.FirstOrDefault(queuedTask => CanbeDispatched(state, locallyExecutingMessages, queuedTask));
            return dispatchingTask != null;
        }

        bool CanbeDispatched(IGlobalBusStateSnapshot state, IReadOnlyList<IMessage> locallyExecutingMessages, DispatchingTask queuedTask)
        {
            return !queuedTask.IsDispatching && _dispatchingRules.All(rule => rule.CanBeDispatched(state, locallyExecutingMessages, queuedTask.Message));
        }
    }
}
