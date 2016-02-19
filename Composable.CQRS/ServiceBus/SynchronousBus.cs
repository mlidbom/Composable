using System;
using Castle.MicroKernel.Lifestyle;
using Castle.Windsor;
using Composable.KeyValueStorage.Population;
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
        private readonly MessageHandlersResolver _handlersResolver;
        private readonly MessageHandlersInvoker _messageHandlersInvoker;
        public SynchronousBus(IWindsorContainer container)
        {
            _container = container;
            _handlersResolver = new MessageHandlersResolver(container: container,
                handlerInterfaces: new[] { typeof(IHandleInProcessMessages<>), typeof(IHandleMessages<>) },
                excludedHandlerInterfaces: new[] { typeof(IHandleRemoteMessages<>) });
            _messageHandlersInvoker = new MessageHandlersInvoker(container, _handlersResolver);
        }

        public virtual void Publish(object message)
        {
            PublishLocal(message);
        }

        public virtual bool Handles(object message)
        {
            return _handlersResolver.HasHandlerFor(message);
        }

        protected virtual void PublishLocal(object message)
        {
            _messageHandlersInvoker.InvokeHandlers(message, allowMultipleHandlers: true);
        }


        protected virtual void SyncSendLocal(object message)
        {
            _messageHandlersInvoker.InvokeHandlers(message, allowMultipleHandlers: false);
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

        //Review:mlidbo: This is not OK. Find a better way of handling this.
        public virtual void SendAtTime(DateTime sendAt, object message) { throw new NotImplementedException(); }
    }
}
