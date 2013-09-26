using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Castle.Windsor;
using Composable.KeyValueStorage.Population;
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
            ((dynamic)this).SyncSendLocal((dynamic)message);
        }

        public virtual bool Handles<TMessage>(TMessage message) where TMessage : IMessage
        {
            return GetHandlerTypes(message).Any();
        }

        protected virtual void SyncSendLocal<TMessage>(TMessage message) where TMessage : IMessage
        {

            var handlerTypes = GetHandlerTypes(message);

            var handlers = new List<object>();
            foreach (var handlerType in handlerTypes)
            {
                handlers.AddRange(ServiceLocator.ResolveAll(handlerType).Cast<object>());
            }

            using(var transactionalScope = ServiceLocator.BeginTransactionalUnitOfWorkScope())
            {
                foreach(dynamic handler in handlers)
                {
                    handler.Handle((dynamic)message);
                }
                transactionalScope.Commit();
            }
        }

        private static IEnumerable<Type> GetHandlerTypes<TMessage>(TMessage message) where TMessage : IMessage
        {
            return message.GetType().GetAllTypesInheritedOrImplemented()
                .Where(t => t.Implements(typeof(IMessage)))
                .Select(t => typeof(IHandleMessages<>).MakeGenericType(t))
                .ToArray();
        }

        public virtual void SendLocal(object message)
        {
            ((dynamic)this).SyncSendLocal((dynamic)message);
        }

        public virtual void Send(object message)
        {
            ((dynamic)this).SyncSendLocal((dynamic)message);
        }

        public virtual void Reply(object message)
        {
            ((dynamic)this).SyncSendLocal((dynamic)message);
        }
    }
}
