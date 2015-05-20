using Castle.Windsor;
using Composable.System.Linq;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus;

namespace Composable.ServiceBus
{
    public interface IMessageHandlerResolversProvider
    {
        List<MessageHandlerResolver> GetResolvers();
    }

    /// <summary>
    /// Provides resolvers for message handlers implementing <see cref="IHandleMessages{T}"/> and <see cref="IHandleInProcessMessages{T}"/>
    /// </summary>
    public class DefaultMessageHandlerResolversProvider : IMessageHandlerResolversProvider
    {
        private readonly IWindsorContainer _container;

        public DefaultMessageHandlerResolversProvider(IWindsorContainer container)
        {
            _container = container;
        }

        public List<MessageHandlerResolver> GetResolvers()
        {
            return new List<MessageHandlerResolver>
                         {
                             new InProcessMessageHandlerResolver(_container),
                             new DefaultMessageHandlerResolver(_container)
                         };
        }
    }

    [UsedImplicitly]
    public class WindsorContainerizedMessageHandlerResolversProvider : IMessageHandlerResolversProvider
    {
        private readonly IWindsorContainer _container;

        public WindsorContainerizedMessageHandlerResolversProvider(IWindsorContainer container)
        {
            _container = container;
        }

        public List<MessageHandlerResolver> GetResolvers()
        {
            var resolvers=_container.ResolveAll<MessageHandlerResolver>().ToList();
            if(resolvers.None())
            {
                throw new NoMessageHandlerResolverRegisteredInContainer();
            }
            return resolvers;
        }
    }

    public class NoMessageHandlerResolverRegisteredInContainer : Exception
    {
        public NoMessageHandlerResolverRegisteredInContainer():base("No MessageHandlerResolver components registered in castle container")
        {
            
        }
    }
}
