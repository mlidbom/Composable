using System;

namespace Composable.Messaging.Buses.Implementation
{
    class NoHandlerException : Exception
    {
        public NoHandlerException(Type messageType) : base($"No handler registered for queuedMessageInformation type: {messageType.FullName}") { }
    }
}
