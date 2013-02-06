using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.CQRS.UI.CommandService;

namespace Composable.Windsor
{
    public class UICommandServiceInstaller : IWindsorInstaller
    {
        public void Install(
            IWindsorContainer container,
            IConfigurationStore store)
        {
            container.Register(
                Component
                    .For<IUICommandService>()
                    .ImplementedBy<UICommandService>()
                    .LifestyleSingleton());
        }
    }
}