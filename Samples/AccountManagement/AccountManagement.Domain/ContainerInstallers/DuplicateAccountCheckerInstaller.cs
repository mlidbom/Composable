using AccountManagement.Domain.Services;
using Composable.DependencyInjection;

namespace AccountManagement.Domain.ContainerInstallers
{
    static class DuplicateAccountCheckerInstaller
    {
        internal static void Install(IDependencyInjectionContainer container)
        {
            container.Register(
                CComponent.For<IDuplicateAccountChecker>()
                    .ImplementedBy<DuplicateAccountChecker>()
                    .LifestyleScoped()
                );
        }
    }
}
