using System;

namespace Composable.Messaging.Buses
{
    partial class ServiceBus
    {
        class DispatchingTask
        {
            public IMessage Message { get; }
            public IMessageDispatchingTracker MessageDispatchingTracker { get; }
            public Action DispatchMessageTask { get; }
            public bool IsDispatching { get; set; }

            public DispatchingTask(IMessage message, IMessageDispatchingTracker messageDispatchingTracker, Action dispatchMessageTask)
            {
                Message = message;
                MessageDispatchingTracker = messageDispatchingTracker;
                DispatchMessageTask = dispatchMessageTask;
            }
        }
    }
}
