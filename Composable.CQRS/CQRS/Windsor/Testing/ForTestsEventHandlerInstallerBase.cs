using System;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using JetBrains.Annotations;
using NServiceBus;

namespace Composable.CQRS.Windsor.Testing
{
    [UsedImplicitly]
    [Obsolete("This class is obsolete and will soon be removed. Please use Composable.Windsor.Testing.ForTestsEventHandlerInstallerBase instead. Search and replace 'using Composable.CQRS.Windsor.Testing;' with 'using Composable.Windsor.Testing;'", error:true)]
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