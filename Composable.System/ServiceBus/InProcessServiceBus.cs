namespace Composable.ServiceBus
{
  using Composable.CQRS.Command;
  using Composable.CQRS.EventSourcing;

  using JetBrains.Annotations;

  [UsedImplicitly] public class InProcessServiceBus : IInProcessServiceBus
  {
    readonly IMessageHandlerRegistry _handlerRegistry;

    readonly object _lock = new object();

    public InProcessServiceBus(IMessageHandlerRegistry handlerRegistry) { this._handlerRegistry = handlerRegistry; }

    public void Publish(IEvent anEvent)
    {
      _handlerRegistry.CreateEventDispatcher()
                      .Dispatch(anEvent);
      AfterDispatchingMessage(anEvent);
    }

    public void Send(ICommand message)
    {
      _handlerRegistry.GetHandlerFor(message)(message);
      AfterDispatchingMessage(message);
    }

    public bool Handles(object aMessage) { return _handlerRegistry.Handles(aMessage); }

    protected virtual void AfterDispatchingMessage(IMessage message) { }
  }
}
