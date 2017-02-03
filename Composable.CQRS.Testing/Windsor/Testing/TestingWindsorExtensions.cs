using System;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.GenericAbstractions.Time;
using Composable.ServiceBus;
using Composable.System.Configuration;
using Composable.Windsor.Testing;

namespace Composable.CQRS.Testing.Windsor.Testing
{
    public static class TestingWindsorExtensions
    {
        public static IWindsorContainer SetupForTesting(this IWindsorContainer @this, Action<IWindsorContainer> registerComponents )
        {
            @this.ConfigureWiringForTestsCallBeforeAllOtherWiring();

            @this.Register(
                Component.For<MessageSpy, IHandleMessages<IMessage>>().Instance(new MessageSpy()),
                Component.For<IUtcTimeTimeSource, DummyTimeSource>().Instance(DummyTimeSource.Now).LifestyleSingleton(),
                Component.For<IServiceBus>().ImplementedBy<SynchronousBus>().LifestylePerWebRequest(),
                Component.For<IWindsorContainer>().Instance(@this),
                Component.For<IConnectionStringProvider>().Instance(new ConnectionStringConfigurationParameterProvider()).LifestyleSingleton()
                );

            registerComponents(@this);
            
            @this.ConfigureWiringForTestsCallAfterAllOtherWiring();

            return @this;
        }
    }
}