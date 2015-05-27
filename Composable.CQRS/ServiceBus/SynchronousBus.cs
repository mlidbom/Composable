using System;
using System.Linq;
using Castle.MicroKernel.Lifestyle;
using Castle.Windsor;
using Composable.KeyValueStorage.Population;
using Composable.System;
using Composable.System.Linq;
using JetBrains.Annotations;
using NServiceBus;

namespace Composable.ServiceBus
{
    /// <summary>
    /// Sends/Publishes messages to <see cref="IHandleMessages{T}"/> implementations registered in the <see cref="IWindsorContainer"/>.
    /// </summary>
    [UsedImplicitly]
    public partial class SynchronousBus : IServiceBus
    {
        private readonly IWindsorContainer _container;
        private readonly MessageHandlerResolver _handlerResolver;
        private  bool _isReplaying = false;

        public SynchronousBus(IWindsorContainer container)
        {
            _container = container;
            _handlerResolver = new MessageHandlerResolver(container);
        }

        public virtual void Publish(object message)
        {
            PublishLocal(message);
        }

        public virtual bool Handles(object message)
        {
            return _handlerResolver.HasHandlerFor(message);
        }

        protected virtual void PublishLocal(object message)
        {
            DispatchMessageToHandlers(message, MessageDispatchType.Publish);
        }

        protected virtual void SyncSendLocal(object message)
        {
            DispatchMessageToHandlers(message, MessageDispatchType.Send);
        }

        public virtual void SendLocal(object message)
        {
            SyncSendLocal(message);
        }

        public virtual void Send(object message)
        {
            SyncSendLocal(message);
        }

        public virtual void Reply(object message)
        {
            SyncSendLocal(message);
        }

        public virtual void Replay(object message)
        {
            using (_container.RequireScope()) //Use the existing scope when running in an endpoint and create a new one if running in the web
            {
                using (var transactionalScope = _container.BeginTransactionalUnitOfWorkScope())
                {
                    var handlers = _handlerResolver.GetHandlers(message, MessageDispatchType.Replay).ToArray();
                    try
                    {
                        _isReplaying = true;
                        foreach (var messageHandlerReference in handlers)
                        {
                            messageHandlerReference.InvokeHandlers(message);
                        }
                        transactionalScope.Commit();
                    }
                    finally
                    {
                        handlers.ForEach(_container.Release);
                        _isReplaying = false;
                    }
                }
            }
        }

        private void DispatchMessageToHandlers<TMessage>(TMessage message, MessageDispatchType dispatchType)
        {
            using(_container.RequireScope()) //Use the existing scope when running in an endpoint and create a new one if running in the web
            {
                using(var transactionalScope = _container.BeginTransactionalUnitOfWorkScope())
                {
                    var handlers = _handlerResolver.GetHandlers(message,dispatchType).ToArray();
                    try
                    {
                        AssertItIsNotReplaying(message);
                        if(dispatchType == MessageDispatchType.Send)
                        {
                            AssertThatThereIsExactlyOneRegisteredHandler(handlers, message);
                        }

                        foreach(var messageHandlerReference in handlers)
                        {
                            messageHandlerReference.InvokeHandlers(message);
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

        private static void AssertThatThereIsExactlyOneRegisteredHandler(MessageHandlerResolver.MessageHandlerReference[] handlers, object message)
        {
            if(handlers.Length == 0)
            {
                throw new NoHandlerException(message.GetType());
            }
            if(handlers.Length > 1)
            {
                var realHandlers = handlers.Select(handler => handler.Instance)
                    .Where(handler => !(handler is ISynchronousBusMessageSpy))
                    .ToList();
                if(realHandlers.Count > 1)
                {
                    throw new MultipleMessageHandlersRegisteredException(message, realHandlers);
                }
            }
        }

        private  void AssertItIsNotReplaying(object message)
        {
            if(_isReplaying)
                throw new CanNotPublishMessageWhenReplayingEventsOnBusException(message); 
        }

        private enum MessageDispatchType
        {
            Publish,
            Send,
            Replay,
        }
    }
}
