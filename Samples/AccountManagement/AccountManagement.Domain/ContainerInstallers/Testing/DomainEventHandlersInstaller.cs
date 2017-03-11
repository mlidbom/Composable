using AccountManagement.Domain.Events.PropertyUpdated;
using AccountManagement.Domain.QueryModels.Updaters;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using JetBrains.Annotations;

namespace AccountManagement.Domain.ContainerInstallers.Testing
{
  using Composable.Messaging.Buses;

  [UsedImplicitly] public class DomainEventHandlersInstaller : IWindsorInstaller
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
