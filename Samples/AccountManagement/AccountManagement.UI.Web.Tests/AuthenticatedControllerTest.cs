using Castle.MicroKernel.Registration;
using Composable.GenericAbstractions.Time;
using NUnit.Framework;

namespace AccountManagement.UI.Web.Tests
{
    public abstract class AuthenticatedControllerTest : ControllerTest
    {
        protected TestAuthenticationContext AuthenticationContext { get; set; }

        [SetUp]
        public void SetupAuthenticationContext()
        {
            AuthenticationContext = new TestAuthenticationContext();
            Container.Register(
                Component.For<TestAuthenticationContext, IAuthenticationContext>().Instance(AuthenticationContext)
                );
        }
    }
}
