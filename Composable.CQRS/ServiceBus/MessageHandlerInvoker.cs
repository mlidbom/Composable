using System.Linq;
using Castle.Windsor;
using Composable.System.Linq;

namespace Composable.ServiceBus
{
    internal class MessageHandlerInvoker
    {
        private readonly IWindsorContainer _container;
        private readonly MyMessageHandlerResolver _handlerResolver;

        public MessageHandlerInvoker(IWindsorContainer container)
        {
            _handlerResolver = new MyMessageHandlerResolver(container);
            _container = container;
        }

        internal bool Handles(object message)
        {
            return _handlerResolver.Handles(message);
        }

        internal void Send<TMessage>(TMessage message)
        {
            InternalInvoke(message, isSend:true);
        }

        internal void Publish<TMessage>(TMessage message)
        {
            InternalInvoke(message, isSend:false);
        }

        private void InternalInvoke<TMessage>(TMessage message, bool isSend = false)
        {
            var handlers = _handlerResolver.GetHandlers(message).ToArray();
            try
            {                
                if(isSend)
                {
                    AssertThatThereIsExactlyOneRegisteredHandler(handlers, message);
                }

                foreach(var messageHandlerReference in handlers)
                {
                    MessageHandlerMethodInvoker.InvokeHandlerMethods(messageHandlerReference.Instance, message, messageHandlerReference.HandlerInterfaceType);
                }

            }
            finally
            {
                handlers.ForEach(_container.Release);
            }
        }

        private void AssertThatThereIsExactlyOneRegisteredHandler(MyMessageHandlerResolver.MessageHandlerReference[] handlers, object message)
        {
            if (handlers.Length == 0)
            {
                throw new NoHandlerException(message.GetType());
            }
            if (handlers.Length > 1)
            {
                var realHandlers = handlers.Select(handler => handler.Instance)
                    .Where(handler => !(handler is ISynchronousBusMessageSpy))
                    .ToList();
                if (realHandlers.Count > 1)
                {
                    throw new MultipleMessageHandlersRegisteredException(message, realHandlers);
                }
            }
        }        
    }
}
