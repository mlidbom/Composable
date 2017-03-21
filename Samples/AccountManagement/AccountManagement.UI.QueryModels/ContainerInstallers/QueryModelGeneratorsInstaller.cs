using AccountManagement.UI.QueryModels.EventStoreGenerated;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.ContainerInstallers
{
    [UsedImplicitly]
    public class QueryModelGeneratorsInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Component.For<AccountQueryModelGenerator>().LifestyleScoped());
        }
    }
}
