using System;
using System.Threading.Tasks;

namespace Composable.Messaging.Buses
{
    partial class InterprocessServiceBus : IInterProcessServiceBus
    {
        class DispatchingTask
        {
            public IMessage Message { get; }
            public Task DispatchMessageTask { get; }

            public DispatchingTask(IMessage message, Action dispatchMessageTask)
            {
                Message = message;
                DispatchMessageTask = new Task(dispatchMessageTask);
            }
        }
    }
}
