using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.CQRS.Query.Models.Generators;

namespace AccountManagement.UI.QueryModels.EventStore.Generators.ContainerInstallers
{
    public class QueryModelGeneratorsInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Classes.FromThisAssembly().BasedOn(typeof(IQueryModelGenerator))
                    .WithServiceBase()
                    .LifestylePerWebRequest()
                );
        }
    }
}