using System;
using System.Threading.Tasks;

namespace Composable.Messaging.Buses
{
    partial class ServiceBus
    {
        class DispatchingTask
        {
            Action _completeAction;
            public IMessage Message { get; }
            public IMessageDispatchingTracker MessageDispatchingTracker { get; }
            public Action DispatchMessageTask { get; }
            public bool IsDispatching { get; set; }

            public void Complete() => Task.Run(_completeAction);

            public DispatchingTask(IMessage message, IMessageDispatchingTracker messageDispatchingTracker, Action dispatchMessageTask, Action completeAction = null)
            {
                _completeAction = completeAction ?? (() => {});
                Message = message;
                MessageDispatchingTracker = messageDispatchingTracker;
                DispatchMessageTask = dispatchMessageTask;
            }
        }
    }
}
