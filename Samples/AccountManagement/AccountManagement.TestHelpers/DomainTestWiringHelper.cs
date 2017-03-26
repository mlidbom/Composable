using AccountManagement.Domain;
using AccountManagement.Domain.Events.EventStore;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Windsor;

namespace AccountManagement.TestHelpers
{
    public static class DomainTestWiringHelper
    {
        public static IServiceLocator SetupContainerForTesting()
        {
            return WindsorDependencyInjectionContainerFactory.SetupForTesting(container =>
                                                                       {
                                                                           AccountManagementDomainBootstrapper.SetupForTesting(container);
                                                                           AccountManagementDomainEventStoreBootstrapper.BootstrapForTesting(container);
                                                                       });
        }
    }
}
