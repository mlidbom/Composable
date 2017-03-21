using AccountManagement.Domain.Services;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using JetBrains.Annotations;

namespace AccountManagement.Domain.ContainerInstallers
{
    [UsedImplicitly]
    public class DuplicateAccountCheckerInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component.For<IDuplicateAccountChecker>()
                    .ImplementedBy<DuplicateAccountChecker>()
                    .LifestyleScoped()
                );
        }
    }
}
