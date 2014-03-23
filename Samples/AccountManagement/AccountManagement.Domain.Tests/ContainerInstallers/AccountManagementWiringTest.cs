using AccountManagement.Domain.Services;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;
using Composable.CQRS.Windsor.Testing;
using Composable.ServiceBus;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests.ContainerInstallers
{     
    public abstract class AccountManagementWiringTest
    {
        //Note that we do NOT test individual classes. We verify that when used as it will really be used things work as expected. 
        //Should we change which installers exist, split installers, merge installers, 
        //etc this test will keep working because it does not care about which installers exist or how exactly they work. 
        //It only cares that things works correctly.
        protected IWindsorContainer Container;

        [SetUp]
        public void WireContainer()
        {
            Container = new WindsorContainer();
            Container.ConfigureWiringForTestsCallBeforeAllOtherWiring();            
            Container.Install(
                FromAssembly.Containing<AccountManagement.Domain.ContainerInstallers.AccountManagementDomainEventStoreInstaller>()
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

        [Test]
        public void EventStoreIsRegisteredScoped()
        {
            Container
                .RegistrationAssertionHelper()
                .LifestyleScoped<IAccountManagementEventStoreSession>();
        }
    }

    [TestFixture]
    public class AccountManagementProductionWiringTest : AccountManagementWiringTest
    {
    }

    [TestFixture]
    public class AccountManagementTestWiringTest : AccountManagementWiringTest
    {
        [SetUp]
        public void SetupTask()
        {
            Container.ConfigureWiringForTestsCallAfterAllOtherWiring();
        }
    }
}