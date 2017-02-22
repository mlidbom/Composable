using Castle.Windsor;
using Castle.Windsor.Installer;
using Composable.CQRS.Testing.Windsor.Testing;
using Composable.Windsor.Testing;

namespace AccountManagement.TestHelpers
{
    public static class DomainTestWiringHelper
    {
        public static IWindsorContainer SetupContainerForTesting()
        {
            return new WindsorContainer()
                .SetupForTesting(@this =>
                                 {
                                     @this.Install(FromAssembly.Containing<Domain.ContainerInstallers.AccountRepositoryInstaller>(),
                                                   FromAssembly.Containing<Domain.Events.EventStore.ContainerInstallers.AccountManagementDomainEventStoreInstaller>());
                                 });
        }
    }
}
