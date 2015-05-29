using System.Linq;
using Castle.MicroKernel.Lifestyle;
using Castle.Windsor;
using Composable.KeyValueStorage.Population;
using Composable.System.Linq;

namespace Composable.ServiceBus
{
    internal class MessageHandlersInvoker
    {
        private readonly IWindsorContainer _container;
        private readonly MessageHandlersResolver _handlersResolver;

        public MessageHandlersInvoker(IWindsorContainer container, MessageHandlersResolver handlersResolver)
        {
            _container = container;
            _handlersResolver = handlersResolver;
        }

        public void InvokeHandlers(object message, bool allowMultipleHandlers)
        {
            using (_container.RequireScope()) //Use the existing scope when running in an endpoint and create a new one if running in the web
            {
                using (var transactionalScope = _container.BeginTransactionalUnitOfWorkScope())
                {
                    var handlers = _handlersResolver.GetHandlers(message).ToArray();
                    try
                    {
                        if (!allowMultipleHandlers)
                        {
                            AssertThatThereIsExactlyOneRegisteredHandler(handlers, message);
                        }

                        foreach (var messageHandlerReference in handlers)
                        {
                            messageHandlerReference.Invoke(message);
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



        private static void AssertThatThereIsExactlyOneRegisteredHandler(MessageHandlersResolver.MessageHandlerReference[] handlers, object message)
        {
            if (handlers.Length == 0)
            {
                throw new NoHandlerException(message.GetType());
            }
            if (handlers.Length > 1)
            {
                //TODO: Maybe we can avoid these code.
                var realHandlers = handlers.Select(handler => handler.ImplementingClass)
                    .Where(handlerType => !typeof(IMessageSpy).IsAssignableFrom(handlerType))
                    .ToList();
                if (realHandlers.Count > 1)
                {
                    throw new MultipleMessageHandlersRegisteredException(message, realHandlers);
                }
            }
        }
    }
}
