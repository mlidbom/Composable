using AccountManagement.UI.QueryModels.EventStore.Generators.Services;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.EventStore.Generators.ContainerInstallers
{
    [UsedImplicitly]
    public class QueryModelGeneratorsInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                //Note the use of a custom interface. This lets us keep query model generators for different systems apart in the wiring easily.
                Classes.FromThisAssembly().BasedOn(typeof(IAccountManagementQueryModelGenerator))
                    .WithServiceBase()
                    .LifestylePerWebRequest()
                );
        }
    }
}