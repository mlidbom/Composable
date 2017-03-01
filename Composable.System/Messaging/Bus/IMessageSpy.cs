namespace Composable.Messaging.Bus
{
  using global::System.Collections.Generic;

  public interface IMessageSpy
    {
        IEnumerable<IMessage> DispatchedMessages { get; }
    }
}
