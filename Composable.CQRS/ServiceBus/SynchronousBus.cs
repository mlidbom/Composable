using Castle.MicroKernel.Lifestyle;
using Castle.Windsor;
using Composable.KeyValueStorage.Population;
using JetBrains.Annotations;
using NServiceBus;

namespace Composable.ServiceBus
{
    /// <summary>
    /// Sends/Publishes messages to <see cref="IHandleMessages{T}"/> implementations registered in the <see cref="IWindsorContainer"/>.
    /// </summary>
    [UsedImplicitly]
    public class SynchronousBus : IServiceBus
    {
        protected readonly IWindsorContainer Container;
        private readonly MessageHandlerInvoker _messageInvoker;

        public SynchronousBus(IWindsorContainer container)
        {
            Container = container;
            _messageInvoker = new MessageHandlerInvoker(container);
        }

        public virtual void Publish(object message)
        {
            PublishLocal(message);
        }

        public virtual bool Handles(object message)
        {
            return _messageInvoker.Handles(message);
        }

        protected virtual void PublishLocal(object message)
        {
            using(Container.RequireScope()) //Use the existing scope when running in an endpoint and create a new one if running in the web
            {
                using(var transactionalScope = Container.BeginTransactionalUnitOfWorkScope())
                {
                    _messageInvoker.Publish(message);
                    transactionalScope.Commit();
                }
            }
        }

        protected virtual void SyncSendLocal(object message)
        {
            using(Container.RequireScope()) //Use the existing scope when running in an endpoint and create a new one if running in the web
            {
                using(var transactionalScope = Container.BeginTransactionalUnitOfWorkScope())
                {
                    _messageInvoker.Send(message);
                    transactionalScope.Commit();
                }
            }
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
    }
}
