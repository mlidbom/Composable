using System;
using Composable.DependencyInjection;

namespace Composable.Messaging.Buses
{
    public class MessageHandlerRegistrarWithDependencyInjectionSupport
    {
        public MessageHandlerRegistrarWithDependencyInjectionSupport(IMessageHandlerRegistrar register, Lazy<IServiceLocator> serviceLocator)
        {
            Register = register;
            ServiceLocator = serviceLocator;
        }

        internal IMessageHandlerRegistrar Register { get; }

        internal Lazy<IServiceLocator> ServiceLocator { get; }

        internal TService Resolve<TService>() where TService : class => ServiceLocator.Value.Resolve<TService>();
    }
}
