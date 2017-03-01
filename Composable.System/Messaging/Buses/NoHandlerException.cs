namespace Composable.Messaging.Buses
{
  using Composable.System;

  using global::System;

  public class NoHandlerException : Exception
    {
        public NoHandlerException(Type messageType) : base("No handler registered for message type: {0}".FormatWith(messageType.FullName)) {}
    }
}
