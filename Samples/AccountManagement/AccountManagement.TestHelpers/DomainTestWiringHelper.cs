using AccountManagement.Domain;
using AccountManagement.Domain.Events.EventStore;
using Composable.DependencyInjection;

namespace AccountManagement.TestHelpers
{
    public static class DomainTestWiringHelper
    {
        public static IServiceLocator SetupContainerForTesting()
        {
            return DependencyInjectionContainer.SetupForTesting(container =>
                                                                       {
                                                                           AccountManagementDomainBootstrapper.SetupForTesting(container);
                                                                           AccountManagementDomainEventStoreBootstrapper.BootstrapForTesting(container);
                                                                       });
        }
    }
}
