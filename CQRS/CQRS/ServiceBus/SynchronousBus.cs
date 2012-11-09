using System.Collections.Generic;
using System.Linq;
using Castle.Windsor;
using Composable.System.Reflection;
using Composable.System.Transactions;
using NServiceBus;

namespace Composable.ServiceBus
{
    public class SynchronousBus : IServiceBus
    {
        protected readonly IWindsorContainer ServiceLocator;

        public SynchronousBus(IWindsorContainer serviceLocator)
        {
            ServiceLocator = serviceLocator;
        }

        public virtual void Publish(object message)
        {
            ((dynamic)this).Publish((dynamic)message);
        }

        protected virtual void Publish<TMessage>(TMessage message) where TMessage : IMessage
        {
            var handlerTypes = message.GetType().GetAllTypesInheritedOrImplemented()
                .Where(t => t.Implements(typeof(IMessage)))
                .Select(t => typeof(IHandleMessages<>).MakeGenericType(t))
                .ToArray();


            var handlers = new List<object>();
            foreach (var handlerType in handlerTypes)
            {
                handlers.AddRange(ServiceLocator.ResolveAll(handlerType).Cast<object>());
            }

            InTransaction.Execute(() =>
            {
                foreach (dynamic handler in handlers)
                {
                    handler.Handle((dynamic)message);
                }
            });
        }

        public virtual void SendLocal(object message)
        {
            Publish(message);
        }

        public virtual void Send(object message)
        {
            Publish(message);
        }

        public virtual void Reply(object message)
        {
            Reply(message);
        }
    }
}
