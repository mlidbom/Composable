using AccountManagement.UI.QueryModels.EventStore.Generators.Services;
using AccountManagement.UI.QueryModels.Services;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.CQRS.Windsor;
using Composable.KeyValueStorage;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.EventStore.Generators.ContainerInstallers
{
    [UsedImplicitly]
    public class AccountManagementQueryModelGeneratingQueryModelSessionInstaller : IWindsorInstaller
    {
        public static class ComponentKeys
        {
            public const string Session = "AccountManagement.QueryModels.Generated.IDocumentDbReader";
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Kernel.Resolver.AddSubResolver(new TypedCollectionResolver<IAccountManagementQueryModelGenerator>(container.Kernel));

            container.Register(
                Component.For<IAccountManagementQueryModelsReader>()
                    .ImplementedBy<AccountManagementQueryModelGeneratingQueryModelSession>()
                    .Named(ComponentKeys.Session)
                    .DependsOn(
                            Dependency.OnValue<IDocumentDbSessionInterceptor>(NullOpDocumentDbSessionInterceptor.Instance)
                        )
                    .LifestylePerWebRequest()
                    .IsDefault()
                );
        }
    }
}
