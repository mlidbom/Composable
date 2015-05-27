using System.Linq;
using Castle.MicroKernel.Lifestyle;
using Castle.Windsor;
using Composable.KeyValueStorage.Population;
using Composable.ServiceBus;
using Composable.System.Linq;
using NServiceBus;

namespace Composable.CQRS.EventSourcing
{
    public interface IReplayEvents
    {
        void Replay(IEvent @event);
    }

    public class EventsReplayer : IReplayEvents
    {
        private readonly IWindsorContainer _container;
        private readonly MessageHandlersResolver _handlersResolver;

        public EventsReplayer(IWindsorContainer container)
        {
            _container = container;
            _container = container;
            _handlersResolver = new MessageHandlersResolver(container: container,
                handlerInterfaces: new[] { typeof(IHandleReplayedEvents<>) },
                excludedHandlerInterfaces: new[] { typeof(IHandleRemoteMessages<>) });
        }

        public void Replay(IEvent @event)
        {
            //TODO: Copy from SynchronousBus, maybe we should extract a IEventInvoker/IMessageInvoker interface?

            using (_container.RequireScope()) //Use the existing scope when running in an endpoint and create a new one if running in the web
            {
                using (var transactionalScope = _container.BeginTransactionalUnitOfWorkScope())
                {
                    var handlers = _handlersResolver.GetHandlers(@event).ToArray();
                    try
                    {
                        foreach (var messageHandlerReference in handlers)
                        {
                            messageHandlerReference.InvokeHandlers(@event);
                        }
                        transactionalScope.Commit();
                    }
                    finally
                    {
                        handlers.ForEach(_container.Release);
                    }
                }
            }
        }
    }
}
