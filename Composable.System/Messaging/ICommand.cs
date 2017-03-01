namespace Composable.Messaging
{
  using global::System;

  public interface ICommand : IMessage
    {
        Guid Id { get; }
    }
}
