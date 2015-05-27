using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.ServiceBus;

namespace Composable.CQRS.Windsor
{
    public class InProcessMessageHandlersInstallerBase<TInheritor>:IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Classes.FromAssemblyContaining<TInheritor>().BasedOn(typeof(IHandleInProcessMessages<>)).WithServiceBase().LifestylePerWebRequest());
        }
    }

    public class ReplayEventsHandlersInstallerBase<TInheritor> : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Classes.FromAssemblyContaining<TInheritor>().BasedOn(typeof(IReplayEvents<>)).WithServiceBase().LifestylePerWebRequest());
        }
    }
}
