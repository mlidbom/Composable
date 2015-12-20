using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS.ServiceBus.NServiceBus;
using Composable.CQRS.Windsor.Testing;
using Composable.Windsor.Testing;
using Composable.ServiceBus;
using Composable.System.Linq;
using NUnit.Framework;

namespace AccountManagement.UI.Web.Tests
{
    [TestFixture]
    public class WebApplicationBootstrapperTests
    {
        private WindsorContainer _container;

        [SetUp]
        public void SetupContainer()
        {
            _container = new WindsorContainer();
            _container.ConfigureWiringForTestsCallBeforeAllOtherWiring();
            ApplicationBootstrapper.ConfigureContainer(_container);

            _container.Register(
                Component.For<IServiceBus>().ImplementedBy<SynchronousBus>()
                    .Named("TestReplacementServiceBus")
                    .LifestylePerWebRequest()
                    .IsDefault()
                );

            _container.ConfigureWiringForTestsCallAfterAllOtherWiring();
        }

        [Test]
        public void CanResolveAllComponents()
        {
            _container.RegistrationAssertionHelper()
                .AllComponentsCanBeResolved(
                    ignoredServices: Seq.OfTypes<
                        NServiceBusServiceBus,
                        IServiceBus>());
        }
    }
}
