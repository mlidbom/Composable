namespace Composable.ServiceBus
{
  using Composable.CQRS.Command;
  using Composable.CQRS.EventHandling;
  using Composable.CQRS.EventSourcing;

  using global::System;

  public interface IMessageHandlerRegistry {
    Action<object> GetHandlerFor(ICommand message);

    IEventDispatcher<IEvent> CreateEventDispatcher();

    bool Handles(object aMessage);
  }
}