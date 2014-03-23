using System.Collections.Generic;
using NServiceBus;

namespace AccountManagement.Domain.Tests.AccountTests
{
    public class MessageSpy : IHandleMessages<IMessage>
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