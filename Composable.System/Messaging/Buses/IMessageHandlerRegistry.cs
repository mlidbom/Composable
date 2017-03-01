namespace Composable.Messaging.Buses
{
  using Composable.CQRS.EventSourcing;
  using Composable.Messaging.Events;

  using global::System;

  public interface IMessageHandlerRegistry {
    Action<object> GetHandlerFor(ICommand message);

    IEventDispatcher<IEvent> CreateEventDispatcher();

    bool Handles(object aMessage);
  }
}