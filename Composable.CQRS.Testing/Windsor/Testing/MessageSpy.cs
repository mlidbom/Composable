using System.Collections.Generic;
using Composable.ServiceBus;

namespace Composable.Windsor.Testing
{
    public class MessageSpy : IHandleMessages<IMessage>,
        ISynchronousBusMessageSpy //Keeps the bus from getting angry when more than one listener exists when invoking "Send". A hack that will be changed.
    {
        public MessageSpy()
        {
            ReceivedMessages = new List<IMessage>();
        }

        public void Handle(IMessage message)
        {
            ReceivedMessages.Add(message);
        }

        public IList<IMessage> ReceivedMessages { get; private set; }
    }
}
