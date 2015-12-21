using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.Windsor.Testing;
using JetBrains.Annotations;
using NServiceBus;

namespace Composable.CQRS.Windsor.Testing
{
    [UsedImplicitly]
    public abstract class ForTestsEventHandlerInstallerBase<TInheritor> : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component.For<IConfigureWiringForTests>().ImplementedBy<RegisterEventHandlersForTest<TInheritor>>()
                );
        }
    }

    public class RegisterEventHandlersForTest<TInheritor> : IConfigureWiringForTests
    {
        private readonly IWindsorContainer _container;

        public RegisterEventHandlersForTest(IWindsorContainer container)
        {
            _container = container;
        }

        public void ConfigureWiringForTesting()
        {
            _container.Register(
                Classes.FromAssemblyContaining<TInheritor>()
                    .BasedOn(typeof(IHandleMessages<>))
                    .WithServiceBase()
                    .LifestyleScoped());
        }
    }
}
