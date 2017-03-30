using AccountManagement.Domain;
using Composable.DependencyInjection;

namespace AccountManagement.TestHelpers
{
    public static class DomainTestWiringHelper
    {
        public static IServiceLocator CreateServiceLocator()
        {
            return DependencyInjectionContainer.CreateServiceLocatorForTesting(container =>
                                                                       {
                                                                           AccountManagementDomainBootstrapper.SetupForTesting(container);
                                                                       });
        }
    }
}
