using AccountManagement.Domain.Events.PropertyUpdated;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.Messaging;
using Composable.Windsor.Testing;

namespace AccountManagement.UI.QueryModels.DocumentDB.Updaters.ContainerInstallers.Testing
{
  using Composable.Messaging.Bus;

  public class QueryModelsUpdatersEventHandlersInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                               Component.For<EmailToAccountMapQueryModelUpdater>()
                                        .LifestyleScoped()
                              );

            container.Resolve<IMessageHandlerRegistrar>()
                     .ForEvent<IAccountEmailPropertyUpdatedEvent>(@event => container.Resolve<EmailToAccountMapQueryModelUpdater>()
                                                                                     .Handle(@event));
        }
    }
}
