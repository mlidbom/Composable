using AccountManagement.Domain.Events.PropertyUpdated;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.DocumentDB.Updaters.ContainerInstallers.Testing
{
  using Composable.Messaging.Buses;

  [UsedImplicitly] public class QueryModelsUpdatersEventHandlersInstaller : IWindsorInstaller
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
