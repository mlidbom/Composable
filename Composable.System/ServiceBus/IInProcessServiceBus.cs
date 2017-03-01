namespace Composable.ServiceBus
{
  using Composable.CQRS.Command;
  using Composable.CQRS.EventSourcing;

  public interface IInProcessServiceBus
  {
    void Publish(IEvent anEvent);
    void Send(ICommand message);
  }
}