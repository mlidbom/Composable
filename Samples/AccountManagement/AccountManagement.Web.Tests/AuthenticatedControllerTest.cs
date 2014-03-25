using Castle.MicroKernel.Registration;
using NUnit.Framework;

namespace AccountManagement.Web.Tests
{
    public abstract class AuthenticatedControllerTest : ControllerTest
    {
        protected TestAuthenticationContext AuthenticationContext { get; set; }

        [SetUp]
        public void SetupAuthenticationContext()
        {
            AuthenticationContext = new TestAuthenticationContext();
            Container.Register(
                Component.For<TestAuthenticationContext, IAuthenticationContext>()
                    .Instance(AuthenticationContext)
                );
        }
    }
}
