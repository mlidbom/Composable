using AccountManagement.Domain.Events.EventStore.Services;
using AccountManagement.Domain.Services;
using AccountManagement.TestHelpers.Scenarios;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.Windsor.Testing;
using Composable.GenericAbstractions.Time;
using Composable.Windsor.Testing;
using Composable.ServiceBus;
using Composable.System.Configuration;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests.ContainerInstallers
{
    public abstract class DomainWiringTest
    {
        //Note that we do NOT test individual classes. We verify that when used as it will really be used things work as expected. 
        //If we change which installers exist, split installers, merge installers etc this test will keep working. 
        //We also make a base class that is abstract and inherit it twice in order to reuse the tests for both production and test wiring
        protected IWindsorContainer Container;

        [SetUp]
        public void WireContainer()
        {
            Container = new WindsorContainer();
            Container.ConfigureWiringForTestsCallBeforeAllOtherWiring();

            Container.Register(
                Component.For<IUtcTimeTimeSource, DummyTimeSource>().Instance(DummyTimeSource.Now).LifestyleSingleton(),
                Component.For<IServiceBus>().ImplementedBy<SynchronousBus>().LifestylePerWebRequest(),
                Component.For<IWindsorContainer>().Instance(Container),
                Component.For<IConnectionStringProvider>().Instance(new ConnectionStringConfigurationParameterProvider()).LifestyleSingleton()
                );

            Container.Install(
                FromAssembly.Containing<Domain.ContainerInstallers.AccountRepositoryInstaller>(),
                FromAssembly.Containing<Domain.Events.EventStore.ContainerInstallers.AccountManagementDomainEventStoreInstaller>()
                );            
        }

        [Test]
        public void AllComponentsCanBeResolved()
        {
            Container
                .RegistrationAssertionHelper()
                .AllComponentsCanBeResolved();
        }

        [Test]
        public void EventStoreIsRegisteredScoped()
        {
            Container
                .RegistrationAssertionHelper()
                .LifestyleScoped<IAccountManagementEventStoreSession>();
        }
    }

    [TestFixture] //The production wiring test does not modify the wiring at all
    public class DomainProductionWiringTest : DomainWiringTest {}

    [TestFixture] //The Test wiring test invokes all IConfigureWiringForTest instances in the container so that the wiring is now appropriate for running tests.
    public class DomainTestWiringTest : DomainWiringTest
    {
        [SetUp]
        public void SetupTask()
        {
            Container.ConfigureWiringForTestsCallAfterAllOtherWiring();
        }
    }
}
