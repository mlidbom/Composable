using System;
using Composable.System;

namespace Composable.Messaging.Buses
{
    class NoHandlerException : Exception
    {
        public NoHandlerException(Type messageType) : base("No handler registered for message type: {0}".FormatWith(messageType.FullName)) { }
    }
}
