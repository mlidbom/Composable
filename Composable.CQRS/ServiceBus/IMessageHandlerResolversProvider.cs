using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.Windsor;

namespace Composable.ServiceBus
{
    public interface IMessageHandlerResolversProvider
    {
        List<MessageHandlerResolver> GetResolvers();
    }

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

    public class WindsorContainerizedMessageHandlerResolversProvider : IMessageHandlerResolversProvider
    {
        private readonly IWindsorContainer _container;

        public WindsorContainerizedMessageHandlerResolversProvider(IWindsorContainer container)
        {
            _container = container;
        }

        public List<MessageHandlerResolver> GetResolvers()
        {
            return _container.ResolveAll<MessageHandlerResolver>().ToList();
        }
    }
}
