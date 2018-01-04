using System;

namespace Composable.Messaging.Buses.Implementation
{
    class NoHandlerForMessageTypeException : Exception
    {
        public NoHandlerForMessageTypeException(Type commandType) : base(commandType.FullName) {}
    }
}
