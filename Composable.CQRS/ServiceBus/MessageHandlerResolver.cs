using Castle.Windsor;
using Composable.System.Linq;
using Composable.System.Reflection;
using NServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Composable.ServiceBus
{
    internal class DefaultMessageHandlerResolver : MessageHandlerResolver
    {
        public DefaultMessageHandlerResolver(IWindsorContainer container)
            : base(container) {}

        override protected Type InterfaceType { get { return typeof(IHandleMessages<>); } }

        override protected IEnumerable<Type> GetHandlerTypes(object message)
        {
            return base.GetHandlerTypes(message)
                .Where(i => !typeof(IHandleRemoteMessages<>).IsAssignableFrom(i)) // we dont dispatch remote messages in the synchronous bus.
                .ToArray();
        }
    }

    internal class InProcessMessageHandlerResolver : MessageHandlerResolver
    {
        public InProcessMessageHandlerResolver(IWindsorContainer container)
            : base(container) {}

        override protected Type InterfaceType { get { return typeof(IHandleInProcessMessages<>); } }
    }

    internal abstract class MessageHandlerResolver
    {
        public class MessageHandlers
        {
            public Type HandlerInterfaceType { get; set; }
            public object Message { get; set; }
            public List<object> HandlerInstances { get; set; }
        }

        protected readonly IWindsorContainer Container;
        protected abstract Type InterfaceType { get; }

        protected MessageHandlerResolver(IWindsorContainer container)
        {
            Container = container;
        }

        public MessageHandlers ResolveMessageHandlers<TMessage>(TMessage message)
        {
            var handlers = new List<object>();
            foreach(var handlerType in GetHandlerTypes(message))
            {
                foreach(var handlerInstance in Container.ResolveAll(handlerType).Cast<object>()) //if one handler implements many interfaces, it will be invoked many times.
                {
                    if(handlers.None(h => h.GetType() == handlerInstance.GetType()))
                    {
                        handlers.Add(handlerInstance);
                    }
                }
            }

            return new MessageHandlers {HandlerInterfaceType = InterfaceType, HandlerInstances = handlers, Message = message};
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
