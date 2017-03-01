namespace Composable.Messaging.Buses
{
  using Composable.CQRS.EventSourcing;

  public interface IInProcessServiceBus
  {
    void Publish(IEvent anEvent);
    void Send(ICommand message);
  }
}