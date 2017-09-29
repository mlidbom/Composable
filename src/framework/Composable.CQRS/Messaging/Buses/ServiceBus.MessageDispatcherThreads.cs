using System;
using System.Threading.Tasks;
using Composable.System.Threading.ResourceAccess;
using Composable.System.Transactions;

namespace Composable.Messaging.Buses
{
    partial class ServiceBus
    {
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
                            TransactionScopeCe.Execute(action: () => dispatchingTask.DispatchMessageTask());
                            dispatchingTask.Complete();
                            break;
                        case IQuery _:
                            dispatchingTask.DispatchMessageTask();
                            dispatchingTask.Complete();
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
    }
}
