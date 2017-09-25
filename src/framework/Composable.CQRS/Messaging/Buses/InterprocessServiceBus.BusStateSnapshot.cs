using System.Collections.Generic;
using System.Linq;

namespace Composable.Messaging.Buses
{
    partial class InterprocessServiceBus
    {
        class BusStateSnapshot : IBusStateSnapshot
        {
            public BusStateSnapshot(InterprocessServiceBus bus)
            {
                InFlightMessages = bus._dispatchingTasks.Select(task => task.Message).ToList();
                LocallyExecuting = new List<IMessage>();
            }
            public IReadOnlyList<IMessage> InFlightMessages { get; }
            public IReadOnlyList<IMessage> LocallyExecuting { get; }
        }
    }
}
