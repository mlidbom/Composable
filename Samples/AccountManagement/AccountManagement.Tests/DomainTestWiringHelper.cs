using AccountManagement.Domain;
using Composable.DependencyInjection;

namespace AccountManagement.Tests
{
    public static class DomainTestWiringHelper
    {
        public static IServiceLocator CreateServiceLocator()
        {
            return DependencyInjectionContainer.CreateServiceLocatorForTesting(AccountManagementDomainBootstrapper.SetupForTesting);
        }
    }
}
