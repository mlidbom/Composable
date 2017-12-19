using System;

namespace Composable.Messaging.Buses.Implementation
{
    class NoHandlerForcommandTypeException : Exception
    {
        public NoHandlerForcommandTypeException(Type commandType) : base(commandType.FullName) {}
    }
}
