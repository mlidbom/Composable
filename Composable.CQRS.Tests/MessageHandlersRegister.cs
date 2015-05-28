using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.ServiceBus;
using NServiceBus;

namespace CQRS.Tests
{
    public class MessageHandlersRegister
    {
        public void RegisterMessageHandlersForTestingFromAssemblyContaining<T>(IWindsorContainer container)
        {
            container.Register(
                //WithServiceSelf used for get component by concrete type
                Classes.FromAssemblyContaining<T>().BasedOn(typeof(IHandleMessages<>)).WithServiceAllInterfaces().WithServiceSelf().LifestylePerWebRequest(),
                Classes.FromAssemblyContaining<T>().BasedOn(typeof(IHandleInProcessMessages<>)).WithServiceAllInterfaces().WithServiceSelf().LifestylePerWebRequest(),
                Classes.FromAssemblyContaining<T>().BasedOn(typeof(IHandleReplayedEvents<>)).WithServiceAllInterfaces().WithServiceSelf().LifestylePerWebRequest()
            );

        }
    }
}
