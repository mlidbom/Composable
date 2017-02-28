using AccountManagement.Domain.Events.PropertyUpdated;
using AccountManagement.Domain.QueryModels.Updaters;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.ServiceBus;
using Composable.Windsor.Testing;

namespace AccountManagement.Domain.ContainerInstallers.Testing
{
    public class DomainEventHandlersInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component.For<EmailToAccountMapQueryModelUpdater>().LifestyleScoped()
                );

            container.Resolve<IMessageHandlerRegistrar>()
                     .ForEvent<IAccountEmailPropertyUpdatedEvent>(@event => container.Resolve<EmailToAccountMapQueryModelUpdater>()
                                                                                     .Handle(@event));
        }
    }
}
