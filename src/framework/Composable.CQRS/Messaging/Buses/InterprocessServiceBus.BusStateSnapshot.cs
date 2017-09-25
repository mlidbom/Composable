using System.Collections.Generic;
using System.Linq;

namespace Composable.Messaging.Buses
{
    partial class InterprocessServiceBus : IInterProcessServiceBus
    {
        class BusStateSnapshot : IBusStateSnapshot
        {
            public BusStateSnapshot(InterprocessServiceBus bus)
            {
                LocallyQueued = bus._dispatchingTasks.Select(task => task.Message).ToList();
                LocallyExecuting = new List<IMessage>();
            }
            public IReadOnlyList<IMessage> LocallyQueued { get; }
            public IReadOnlyList<IMessage> LocallyExecuting { get; }
        }
    }
}
