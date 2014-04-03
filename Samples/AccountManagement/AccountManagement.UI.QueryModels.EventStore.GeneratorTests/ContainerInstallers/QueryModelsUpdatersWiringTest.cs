using System;
using AccountManagement.Domain;
using AccountManagement.TestHelpers.Scenarios;
using AccountManagement.UI.QueryModels.DocumentDB.Updaters.Services;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.Windsor;
using Castle.Windsor.Installer;
using Composable.CQRS.EventHandling;
using Composable.CQRS.Windsor.Testing;
using Composable.KeyValueStorage;
using Composable.ServiceBus;
using NUnit.Framework;

namespace AccountManagement.UI.QueryModels.EventStore.Generators.Tests.ContainerInstallers
{
    public abstract class QueryModelsUpdatersWiringTest
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

            Container.Kernel.Resolver.AddSubResolver(new CollectionResolver(Container.Kernel));

            Container.Install(
                FromAssembly.Containing<Domain.Events.EventStore.ContainerInstallers.AccountManagementDomainEventStoreInstaller>(),
                FromAssembly.Containing<UI.QueryModels.EventStore.Generators.ContainerInstallers.AccountManagementQueryModelGeneratingQueryModelSessionInstaller>()
                );

            Container.Register(
                Component.For<IServiceBus>().ImplementedBy<SynchronousBus>().LifestylePerWebRequest(),
                Component.For<IWindsorContainer>().Instance(Container)
                );
        }

        [Test]
        public void AllComponentsCanBeResolved()
        {
            Container
                .RegistrationAssertionHelper()
                .AllComponentsCanBeResolved();
        }
    }

    [TestFixture] //The production wiring test does not modify the wiring at all
    public class QueryModelsUpdatersProductionWiringTest : QueryModelsUpdatersWiringTest {}

    [TestFixture] //The Test wiring test invokes all IConfigureWiringForTest instances in the container so that the wiring is now appropriate for running tests.
    public class QueryModelsUpdatersTestWiringTest : QueryModelsUpdatersWiringTest
    {
        [SetUp]
        public void SetupTask()
        {
            Container.ConfigureWiringForTestsCallAfterAllOtherWiring();
        }
    }
}
