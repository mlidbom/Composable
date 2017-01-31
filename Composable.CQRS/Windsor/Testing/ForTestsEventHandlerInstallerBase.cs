using System;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.ServiceBus;
using JetBrains.Annotations;

namespace Composable.Windsor.Testing
{
    [UsedImplicitly]
    public abstract class ForTestsEventHandlerInstallerBase<TInheritor> : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component.For<Composable.Windsor.Testing.IConfigureWiringForTests>().ImplementedBy<RegisterEventHandlersForTest<TInheritor>>()
                );
        }
    }

    public class RegisterEventHandlersForTest<TInheritor> : Composable.Windsor.Testing.IConfigureWiringForTests
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
                    .BasedOn(typeof(IHandleMessages<>))
                    .WithServiceBase()
                    .LifestyleScoped());
        }
    }
}