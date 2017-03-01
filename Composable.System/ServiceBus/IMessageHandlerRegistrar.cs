namespace Composable.ServiceBus
{
  using Composable.CQRS.Command;
  using Composable.CQRS.EventSourcing;

  using global::System;

  public interface IMessageHandlerRegistrar
  {
    IMessageHandlerRegistrar ForEvent<TEvent>(Action<TEvent> handler) where TEvent : IEvent;
    IMessageHandlerRegistrar ForCommand<TCommand>(Action<TCommand> handler) where TCommand : ICommand;
  }
}