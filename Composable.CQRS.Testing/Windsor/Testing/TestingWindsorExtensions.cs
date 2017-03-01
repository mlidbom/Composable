using System;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.GenericAbstractions.Time;
using Composable.Messaging;
using Composable.System.Configuration;
using Composable.Windsor.Testing;

namespace Composable.CQRS.Testing.Windsor.Testing
{
  using Composable.Messaging.Bus;

  public static class TestingWindsorExtensions
    {
        public static IWindsorContainer SetupForTesting(this IWindsorContainer @this, Action<IWindsorContainer> registerComponents )
        {
            @this.ConfigureWiringForTestsCallBeforeAllOtherWiring();

            var dummyTimeSource = DummyTimeSource.Now;
            var registry = new MessageHandlerRegistry();
          var bus = new TestingOnlyServiceBus(dummyTimeSource, registry);

            @this.Register(
                Component.For<IUtcTimeTimeSource, DummyTimeSource>().Instance(dummyTimeSource).LifestyleSingleton(),
                Component.For<IMessageHandlerRegistrar>().Instance(registry).LifestyleSingleton(),
                Component.For<IServiceBus, IMessageSpy>().Instance(bus).LifestyleSingleton(),
                Component.For<IWindsorContainer>().Instance(@this),
                Component.For<IConnectionStringProvider>().Instance(new ConnectionStringConfigurationParameterProvider()).LifestyleSingleton()
                );            

            registerComponents(@this);
            
            @this.ConfigureWiringForTestsCallAfterAllOtherWiring();

            return @this;
        }
    }
}