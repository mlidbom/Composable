using System;

namespace Composable.Messaging.Buses.Implementation
{
    public class MessageDispatchingFailedException : Exception
    {
        public MessageDispatchingFailedException() : base("Dispatching message failed") {}
    }
}
