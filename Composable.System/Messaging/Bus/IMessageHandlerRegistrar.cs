namespace Composable.Messaging.Bus
{
  using Composable.CQRS.EventSourcing;
  using Composable.Messaging.Commands;

  using global::System;

  public interface IMessageHandlerRegistrar
  {
    IMessageHandlerRegistrar ForEvent<TEvent>(Action<TEvent> handler) where TEvent : IEvent;
    IMessageHandlerRegistrar ForCommand<TCommand>(Action<TCommand> handler) where TCommand : ICommand;
  }
}