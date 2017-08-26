using AccountManagement.Domain;
using Composable.DependencyInjection;
using Composable.Messaging.Buses;

namespace AccountManagement.Tests
{
    public static class DomainTestWiringHelper
    {
        public static IServiceLocator CreateServiceLocator()
        {
            var locator =  DependencyInjectionContainer.CreateServiceLocatorForTesting(AccountManagementDomainBootstrapper.SetupContainer);

            locator.Use<IMessageHandlerRegistrar>(registrar => AccountManagementDomainBootstrapper.RegisterHandlers(registrar, locator));

            return locator;
        }
    }
}
