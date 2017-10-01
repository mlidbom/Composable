using System;

namespace Composable.Messaging.Buses
{
    partial class ServiceBus
    {
        class DispatchingTask
        {
            public readonly IMessage Message;
            public readonly IMessageDispatchingTracker MessageDispatchingTracker;
            public readonly Action DispatchMessageTask;
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
