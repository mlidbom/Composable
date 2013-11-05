using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Castle.Windsor;
using Composable.CQRS;
using Composable.CQRS.EventSourcing;
using Composable.KeyValueStorage.Population;
using Composable.System;
using Composable.System.Linq;
using Composable.System.Reflection;
using Composable.System.Transactions;
using NServiceBus;

namespace Composable.ServiceBus
{
    /// <summary>
    /// Sends/Publishes messages to <see cref="IHandleMessages{T}"/> implementations registered in the <see cref="IWindsorContainer"/>.
    /// 
    /// <para>
    ///     An <see cref="ISynchronousBusSubscriberFilter"/> can be registered in the container to avoid dispatching to some handlers.
    /// </para>
    /// </summary>
    public class SynchronousBus : IServiceBus
    {
        protected readonly IWindsorContainer ServiceLocator;
        private ISynchronousBusSubscriberFilter _subscriberFilter;

        public SynchronousBus(IWindsorContainer serviceLocator, ISynchronousBusSubscriberFilter subscriberFilter = null)
        {
            _subscriberFilter = subscriberFilter ?? PublishToAllSubscribersSubscriberFilter.Instance;
            ServiceLocator = serviceLocator;
        }

        public virtual void Publish(object message)
        {
            ((dynamic)this).PublishLocal((dynamic)message);
        }

        public virtual bool Handles<TMessage>(TMessage message) where TMessage : IMessage
        {
            return GetHandlerTypes(message).Any();
        }

        protected virtual void PublishLocal<TMessage>(TMessage message) where TMessage : IMessage
        {

            var handlerTypes = GetHandlerTypes(message);

            var handlers = new List<object>();
            foreach (var handlerType in handlerTypes)
            {
                handlers.AddRange(ServiceLocator.ResolveAll(handlerType).Cast<object>());
            }

            using (var transactionalScope = ServiceLocator.BeginTransactionalUnitOfWorkScope())
            {
                foreach (dynamic handler in handlers)
                {
                    if(_subscriberFilter.PublishMessageToHandler(message, handler))
                    {
                        handler.Handle((dynamic)message);
                    }
                }
                transactionalScope.Commit();
            }
        }

        protected virtual void SyncSendLocal<TMessage>(TMessage message) where TMessage : IMessage
        {

            var handlerTypes = GetHandlerTypes(message);

            var handlers = new List<object>();
            foreach (var handlerType in handlerTypes)
            {
                handlers.AddRange(ServiceLocator.ResolveAll(handlerType).Cast<object>());
            }

            if(handlers.Count() > 1)
            {
                throw new MultipleMessageHandlersRegisteredException(message, handlers);
            }

            if(handlers.None())
            {
                throw new NoHandlerException(message.GetType());
            }

            using(var transactionalScope = ServiceLocator.BeginTransactionalUnitOfWorkScope())
            {
                ((dynamic)handlers.Single()).Handle((dynamic)message);
                transactionalScope.Commit();
            }
        }

        private IEnumerable<Type> GetHandlerTypes<TMessage>(TMessage message) where TMessage : IMessage
        {
            return message.GetType().GetAllTypesInheritedOrImplemented()
                .Where(t => t.Implements(typeof(IMessage)))
                .Select(t => typeof(IHandleMessages<>).MakeGenericType(t))
                .Where(t => ServiceLocator.Kernel.HasComponent(t))
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

    public class NoHandlerException : Exception
    {
        public NoHandlerException(Type messageType):base("No handler registered for message type: {0}".FormatWith(messageType.FullName))
        {
            
        }
    }

    public class MultipleMessageHandlersRegisteredException : Exception
    {
        public MultipleMessageHandlersRegisteredException(object message, List<object> handlers):base(CreateMessage(message,handlers))
        {
        }

        private static string CreateMessage(object message, List<object> handlers)
        {
            var exceptionMessage = "There are multiple handlers registered for the message type:{0}".FormatWith(message.GetType());
            handlers.Select(handler => handler.GetType())
                .ForEach(handlerType => exceptionMessage += "{0}{1}".FormatWith(Environment.NewLine, handlerType.FullName)                
                );
            return exceptionMessage;

        }
    }

    public class PublishToAllSubscribersSubscriberFilter : ISynchronousBusSubscriberFilter
    {
        public static readonly ISynchronousBusSubscriberFilter Instance = new PublishToAllSubscribersSubscriberFilter();

        public bool PublishMessageToHandler(object message, object handler)
        {
            return true;
        }
    }
}
