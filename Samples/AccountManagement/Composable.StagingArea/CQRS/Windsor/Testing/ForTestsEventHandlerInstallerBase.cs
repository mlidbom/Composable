using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.Messaging;
using Composable.Windsor.Testing;
using JetBrains.Annotations;

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

    class RegisterEventHandlersForTest<TInheritor> : IConfigureWiringForTests
    {
        readonly IWindsorContainer _container;

        public RegisterEventHandlersForTest(IWindsorContainer container)
        {
            _container = container;
        }

        public void ConfigureWiringForTesting()
        {
            _container.Register(
                Classes.FromAssemblyContaining<TInheritor>()
                    .BasedOn(typeof(IEventSubscriber<>))
                    .WithServiceBase()
                    .LifestyleScoped());
        }
    }
}
