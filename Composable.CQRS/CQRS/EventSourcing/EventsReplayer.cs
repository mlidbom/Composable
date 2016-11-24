using Castle.Windsor;
using Composable.ServiceBus;

namespace Composable.CQRS.EventSourcing
{
    public class EventsReplayer : IReplayEvents
    {
        private readonly MessageHandlersInvoker _handlersInvoker;

        public EventsReplayer(IWindsorContainer container)
        {
            var handlerResolver = new MessageHandlersResolver(
                container: container,
                handlerInterfaces: new[] { typeof(IHandleReplayedEvents<>) },
                excludedHandlerInterfaces: new[] { typeof(IHandleRemoteMessages<>) }
            );

            _handlersInvoker = new MessageHandlersInvoker(container, handlerResolver);
        }

        public void Replay(IEvent @event)
        {
            _handlersInvoker.InvokeHandlers(@event, allowMultipleHandlers: true);
        }
    }
}