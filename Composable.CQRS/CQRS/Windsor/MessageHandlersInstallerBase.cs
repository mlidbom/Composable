using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.ServiceBus;
using NServiceBus;

namespace Composable.CQRS.Windsor
{
    [Obsolete("'Now in the Composable.Windsor namespace. Search and replace: 'using Composable.CQRS.Windsor.Testing;' with 'using Composable.Windsor.Testing;' and 'using Composable.CQRS.Windsor;' with 'using Composable.Windsor;'", error: true)]
    public class InProcessMessageHandlersInstallerBase<TInheritor>:IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Classes.FromAssemblyContaining<TInheritor>().BasedOn(typeof(IHandleInProcessMessages<>)).WithServiceBase().LifestylePerWebRequest());
        }
    }
    
}
