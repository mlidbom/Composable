using AccountManagement.UI.QueryModels.Services;
using AccountManagement.UI.QueryModels.Services.Implementation;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.KeyValueStorage;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.ContainerInstallers
{
    [UsedImplicitly]
    public class AccountManagementQueryModelReaderInstaller : IWindsorInstaller
    {
        public static class ComponentKeys
        {
            public const string QueryModelsReader = "AccountManagement.QueryModels.QueryModelsReader";
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component.For<IAccountManagementQueryModelsReader>()
                    .ImplementedBy<AccountManagementQueryModelReader>()
                    .DependsOn(
                        Dependency.OnValue<IDocumentDbSessionInterceptor>(NullOpDocumentDbSessionInterceptor.Instance)
                    )
                    .Named(ComponentKeys.QueryModelsReader)
                    .LifestylePerWebRequest()
                    .IsDefault()
                );
        }
    }
}
