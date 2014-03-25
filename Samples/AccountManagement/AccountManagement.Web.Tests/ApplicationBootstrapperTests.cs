using Castle.Windsor;
using Composable.CQRS.Windsor.Testing;
using NUnit.Framework;

namespace AccountManagement.Web.Tests
{
    [TestFixture]
    public class ApplicationBootstrapperTests
    {
        private WindsorContainer _container;

        [SetUp]
        public void SetupContainer()
        {
            _container = WebTestWiringHelper.CreateContainerWithAuthenticationContext();
        }

        [Test]
        public void CanResolveAllComponents()
        {
            _container.RegistrationAssertionHelper()
                .AllComponentsCanBeResolved();
        }
    }
}