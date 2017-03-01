using System.Collections.Generic;

namespace Composable.ServiceBus
{
    public interface IMessageSpy
    {
        IEnumerable<IMessage> DispatchedMessages { get; }
    }
}
