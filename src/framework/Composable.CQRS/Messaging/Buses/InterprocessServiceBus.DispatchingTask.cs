using System;
using System.Threading.Tasks;

namespace Composable.Messaging.Buses
{
    partial class ServiceBus
    {
        class DispatchingTask
        {
            public IMessage Message { get; }
            public IMessageDispatchingTracker MessageDispatchingTracker { get; }
            public Task DispatchMessageTask { get; }
            public bool IsDispatching { get; set; }

            public DispatchingTask(IMessage message, IMessageDispatchingTracker messageDispatchingTracker, Action dispatchMessageTask)
                :this(message, messageDispatchingTracker, new Task(dispatchMessageTask))
            {

            }

            public DispatchingTask(IMessage message, IMessageDispatchingTracker messageDispatchingTracker, Task dispatchMessageTask)
            {
                Message = message;
                MessageDispatchingTracker = messageDispatchingTracker;
                DispatchMessageTask = dispatchMessageTask;
            }
        }
    }
}
