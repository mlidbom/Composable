using AccountManagement.Domain;
using Composable.DependencyInjection;
using Composable.Messaging.Buses;

namespace AccountManagement.Tests
{
    public static class DomainTestWiringHelper
    {
        public static IServiceLocator CreateServiceLocator()
        {
            var locator =  DependencyInjectionContainer.CreateServiceLocatorForTesting(AccountManagementServerDomainBootstrapper.SetupContainer);

            locator.Use<IMessageHandlerRegistrar>(registrar => AccountManagementServerDomainBootstrapper.RegisterHandlers(new MessageHandlerRegistrarWithDependencyInjectionSupport(registrar, locator), locator));

            return locator;
        }
    }
}
