using AccountManagement.UI.QueryModels.Services;
using AccountManagement.UI.QueryModels.Services.Implementation;
using Composable.DependencyInjection;

namespace AccountManagement.UI.QueryModels.ContainerInstallers
{
    static class AccountManagementQueryModelReaderInstaller
    {
        static class ComponentKeys
        {
            public const string QueryModelsReader = "AccountManagement.QueryModels.QueryModelsReader";
        }

        internal static void Install(IDependencyInjectionContainer container)
        {
            container.Register(
                CComponent.For<IAccountManagementQueryModelsReader>()
                    .ImplementedBy<AccountManagementQueryModelReader>()
                    .Named(ComponentKeys.QueryModelsReader)
                    .LifestyleScoped()
                );
        }
    }
}
