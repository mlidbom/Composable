using Castle.Windsor;
using Composable.System.Linq;
using Composable.System.Reflection;
using NServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Composable.ServiceBus
{
    public class DefaultMessageHandlerResolver : MessageHandlerResolver
    {
        public DefaultMessageHandlerResolver(IWindsorContainer container)
            : base(container) {}

        override public Type InterfaceType { get { return typeof(IHandleMessages<>); } }

        override protected IEnumerable<Type> GetHandlerTypes(object message)
        {
            return base.GetHandlerTypes(message)
                .Where(i => !typeof(IHandleRemoteMessages<>).IsAssignableFrom(i)) // we don't dispatch remote messages in the synchronous bus.
                .ToArray();
        }

        override public List<object> ResolveMessageHandlers<TMessage>(TMessage message)
        {
            var remoteMessageHandlerType = typeof(IHandleRemoteMessages<>).MakeGenericType(message.GetType());
            return base.ResolveMessageHandlers(message)
                // ReSharper disable once UseIsOperator.2
                .Where(h => !remoteMessageHandlerType.IsInstanceOfType(h))
                .ToList();
        }
    }

    public class InProcessMessageHandlerResolver : MessageHandlerResolver
    {
        public InProcessMessageHandlerResolver(IWindsorContainer container)
            : base(container) {}

        override public Type InterfaceType { get { return typeof(IHandleInProcessMessages<>); } }
    }

    public abstract class MessageHandlerResolver
    {
        protected readonly IWindsorContainer Container;
        
        public abstract Type InterfaceType { get; }

        protected MessageHandlerResolver(IWindsorContainer container)
        {
            Container = container;
        }

        public virtual List<object> ResolveMessageHandlers<TMessage>(TMessage message)
        {
            var handlers = new List<object>();
            foreach(var handlerType in GetHandlerTypes(message))
            {
                foreach(var handlerInstance in Container.ResolveAll(handlerType).Cast<object>()) //if one handler implements many interfaces, it will be invoked many times.
                {
                    if(!handlers.Contains(handlerInstance))
                    {
                        handlers.Add(handlerInstance);
                    }
                }
            }

            return handlers;
        }

        public bool HasHandlerFor<TMessage>(TMessage message)
        {
            return GetHandlerTypes(message).Any();
        }

        protected virtual IEnumerable<Type> GetHandlerTypes(object message)
        {
            return message.GetType().GetAllTypesInheritedOrImplemented()
                .Where(m => m.Implements(typeof(IMessage)))
                .Select(m => InterfaceType.MakeGenericType(m))
                .Where(i => Container.Kernel.HasComponent(i))
                .ToArray();

        }
    }
}
