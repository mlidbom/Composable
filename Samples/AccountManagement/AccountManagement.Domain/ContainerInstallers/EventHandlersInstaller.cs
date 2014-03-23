using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Castle.Windsor.Installer;
using Composable.CQRS.Windsor.Testing;
using NServiceBus;

namespace AccountManagement.Domain.ContainerInstallers
{
    public class EventHandlersInstaller:IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component.For<IConfigureWiringForTests>().ImplementedBy<RegisterEventHandlersForTest>()
                );
        }
    }

    public class RegisterEventHandlersForTest : IConfigureWiringForTests
    {
        private readonly IWindsorContainer _container;
        public RegisterEventHandlersForTest(IWindsorContainer container )
        {
            _container = container;
        }

        public void ConfigureWiringForTesting()
        {
            _container.Register(
                Classes.FromThisAssembly()
                    .BasedOn(typeof(IHandleMessages<>))
                    .WithServiceBase()
                    .LifestyleScoped());
        }
    }
}