using Castle.Windsor;
using Composable.System.Linq;
using Composable.System.Reflection;
using NServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Composable.ServiceBus
{
    ///<summary>Resolves message handlers that inherits from <see cref="IHandleMessages{T}"/>.
    /// <remarks>Does not return message handlers that implements <see cref="IHandleRemoteMessages{T}"/>.</remarks>
    /// </summary>
    public class DefaultMessageHandlerResolver : MessageHandlerResolver
    {
        public DefaultMessageHandlerResolver(IWindsorContainer container)
            : base(container) {}

        override public Type HandlerInterfaceType { get { return typeof(IHandleMessages<>); } }

        override protected IEnumerable<Type> GetHandlerTypes(object message)
        {
            return base.GetHandlerTypes(message)
                // We don't dispatch messages to a IHandleRemoteMessages handler in the synchronous bus.
                .Where(i => !typeof(IHandleRemoteMessages<>).IsAssignableFrom(i))
                .ToArray();
        }

        override public List<object> ResolveMessageHandlers<TMessage>(TMessage message)
        {
            var remoteMessageHandlerType = typeof(IHandleRemoteMessages<>).MakeGenericType(message.GetType());
            return base.ResolveMessageHandlers(message)
                // ReSharper disable once UseIsOperator.2
                // A IHandleRemoteMessages handler might be resolved in case someone wired in a IHandleMessages handler for the same IMessage because of the inheritence.
                // We don't want to dispatch messages to a IHandleRemoteMessages handler.
                .Where(h => !remoteMessageHandlerType.IsInstanceOfType(h)) 
                .ToList();
        }
    }

    ///<summary>Resolves message handlers that inherits from <see cref="IHandleInProcessMessages{T}"/>.</summary>
    public class InProcessMessageHandlerResolver : MessageHandlerResolver
    {
        public InProcessMessageHandlerResolver(IWindsorContainer container)
            : base(container) {}

        override public Type HandlerInterfaceType { get { return typeof(IHandleInProcessMessages<>); } }
    }

    public abstract class MessageHandlerResolver
    {
        protected readonly IWindsorContainer Container;
        
        public abstract Type HandlerInterfaceType { get; }

        protected MessageHandlerResolver(IWindsorContainer container)
        {
            Container = container;
        }

        public virtual List<object> ResolveMessageHandlers<TMessage>(TMessage message)
        {
            var handlers = new List<object>();
            foreach(var handlerType in GetHandlerTypes(message))
            {
                foreach(var handlerInstance in Container.ResolveAll(handlerType).Cast<object>())
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
                .Select(m => HandlerInterfaceType.MakeGenericType(m))
                .Where(i => Container.Kernel.HasComponent(i))
                .ToArray();

        }
    }
}
