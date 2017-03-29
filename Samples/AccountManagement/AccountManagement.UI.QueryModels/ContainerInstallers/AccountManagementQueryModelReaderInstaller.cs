using AccountManagement.UI.QueryModels.Services;
using AccountManagement.UI.QueryModels.Services.Implementation;
using Composable.DependencyInjection;

namespace AccountManagement.UI.QueryModels.ContainerInstallers
{
    static class AccountManagementQueryModelReaderInstaller
    {
        internal static void Install(IDependencyInjectionContainer container)
        {
            container.Register(
                CComponent.For<IAccountManagementQueryModelsReader>()
                    .ImplementedBy<AccountManagementQueryModelReader>()
                    .LifestyleScoped()
                );
        }
    }
}
