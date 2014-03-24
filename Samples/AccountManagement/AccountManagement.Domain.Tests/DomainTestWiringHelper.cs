using AccountManagement.Domain.Tests.AccountTests;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;
using Composable.CQRS.Windsor.Testing;
using Composable.ServiceBus;
using NServiceBus;

namespace AccountManagement.Domain.Tests
{
    public static class DomainTestWiringHelper
    {
        public static WindsorContainer SetupContainerForTesting()
        {
            var container = new WindsorContainer();
            container.ConfigureWiringForTestsCallBeforeAllOtherWiring();
            container.Install(FromAssembly.Containing<Domain.ContainerInstallers.AccountManagementDomainEventStoreInstaller>());

            container.Register(
                Component.For<MessageSpy, IHandleMessages<IMessage>>().Instance(new MessageSpy())
                );

            container.Register(
                Component.For<IServiceBus>().ImplementedBy<SynchronousBus>().LifestylePerWebRequest(),
                Component.For<IWindsorContainer>().Instance(container)
                );

            container.ConfigureWiringForTestsCallAfterAllOtherWiring();
            return container;
        }
    }
}