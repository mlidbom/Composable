using System;

namespace Composable.Messaging.Buses
{
    class NoHandlerException : Exception
    {
        public NoHandlerException(Type messageType) : base($"No handler registered for queuedMessageInformation type: {messageType.FullName}") { }
    }
}
