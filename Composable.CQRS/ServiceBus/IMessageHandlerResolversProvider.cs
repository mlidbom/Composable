using System.Collections.Generic;
using Castle.Windsor;
using NServiceBus;

namespace Composable.ServiceBus
{
    public interface IMessageHandlerResolversProvider
    {
        List<MessageHandlerResolver> GetResolvers();
    }

    /// <summary>
    /// Provides default resolvers for message handlers used with <see cref="SynchronousBus"/>. 
    /// <seealso cref="IHandleMessages{T}"/> 
    /// <seealso cref="IHandleInProcessMessages{T}"/>.
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
                             new AllExceptRemoteMessageHandlersMessageHandlerResolver(_container)
                         };
        }
    }    
}
