using System;
using System.Collections.Generic;
using System.Linq;
using Composable.System;

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
                    while(TryGetDispatchableMessage(out var dispatchingTask))
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
