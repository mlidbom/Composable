using System.Collections.Generic;

namespace Composable.Messaging.Buses
{
    public interface IMessageSpy
    {
        IEnumerable<IMessage> DispatchedMessages { get; }
    }
}
