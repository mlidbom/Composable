using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.ServiceBus;
using NServiceBus;

namespace Composable.CQRS.Windsor.Testing
{
    public abstract class ForTestsReplayEventsHandlersInstallerBase<TInheritor> : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component.For<IConfigureWiringForTests>().ImplementedBy<RegisterReplayHandlersForTest<TInheritor>>()
                );
        }
    }

    public class RegisterReplayHandlersForTest<TInheritor> : IConfigureWiringForTests
    {
        private readonly IWindsorContainer _container;

        public RegisterReplayHandlersForTest(IWindsorContainer container)
        {
            _container = container;
        }

        public void ConfigureWiringForTesting()
        {
            _container.Register(
                Classes.FromAssemblyContaining<TInheritor>()
                    .BasedOn(typeof(IHandleMessages<>),typeof(IHandleReplayedEvents<>))
                    .WithServiceBase()
                    .LifestyleScoped());
        }
    }
}
