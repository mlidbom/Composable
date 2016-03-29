using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;
using Composable.GenericAbstractions.Time;
using Composable.Windsor.Testing;
using Composable.ServiceBus;
using Composable.System.Configuration;
using NServiceBus;

namespace AccountManagement.TestHelpers
{
    public static class DomainTestWiringHelper
    {
        public static WindsorContainer SetupContainerForTesting()
        {
            var container = new WindsorContainer();
            container.ConfigureWiringForTestsCallBeforeAllOtherWiring();

            container.Register(
                Component.For<MessageSpy, IHandleMessages<IMessage>>().Instance(new MessageSpy()),
                Component.For<IUtcTimeTimeSource, DummyTimeSource>().Instance(DummyTimeSource.Now).LifestyleSingleton(),
                Component.For<IServiceBus>().ImplementedBy<SynchronousBus>().LifestylePerWebRequest(),
                Component.For<IWindsorContainer>().Instance(container),
                Component.For<IConnectionStringProvider>().Instance(new ConnectionStringConfigurationParameterProvider()).LifestyleSingleton()
                );

            container.Install(
                FromAssembly.Containing<Domain.ContainerInstallers.AccountRepositoryInstaller>(),
                FromAssembly.Containing<Domain.Events.EventStore.ContainerInstallers.AccountManagementDomainEventStoreInstaller>()
                );          

            container.ConfigureWiringForTestsCallAfterAllOtherWiring();
            return container;
        }
    }
}
