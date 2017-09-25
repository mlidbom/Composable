using System;
using System.Threading.Tasks;

namespace Composable.Messaging.Buses
{
    partial class InterprocessServiceBus : IInterProcessServiceBus
    {
        class DispatchingTask
        {
            public IMessage Message { get; }
            public IMessageDispatchingTracker MessageDispatchingTracker { get; }
            public Task DispatchMessageTask { get; }

            public DispatchingTask(IMessage message, IMessageDispatchingTracker messageDispatchingTracker, Action dispatchMessageTask)
            {
                Message = message;
                MessageDispatchingTracker = messageDispatchingTracker;
                DispatchMessageTask = new Task(dispatchMessageTask);
            }
        }
    }
}
