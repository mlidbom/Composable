namespace Composable.Messaging.Bus
{
  using Composable.CQRS.EventSourcing;
  using Composable.Messaging.Commands;

  public interface IInProcessServiceBus
  {
    void Publish(IEvent anEvent);
    void Send(ICommand message);
  }
}