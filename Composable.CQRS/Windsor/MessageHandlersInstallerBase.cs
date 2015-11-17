using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.ServiceBus;

namespace Composable.Windsor
{
    public class InProcessMessageHandlersInstallerBase<TInheritor>:IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Classes.FromAssemblyContaining<TInheritor>().BasedOn(typeof(IHandleInProcessMessages<>)).WithServiceBase().LifestylePerWebRequest());
        }
    }
    
}
