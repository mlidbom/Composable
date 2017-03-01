namespace Composable.Messaging
{
  using Composable.CQRS.EventSourcing;
    public interface IEventHandler<in TEvent> : IEventSubscriber<TEvent> where TEvent : IEvent
    {}
}