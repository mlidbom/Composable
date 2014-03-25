using NUnit.Framework;

namespace AccountManagement.Web.Tests.AccountControllerTests
{
    public abstract class AuthenticatedControllerTest : MvcControllerTest
    {
        protected TestAuthenticationContext AuthenticationContext { get; set; }

        [SetUp]
        public void SetupAuthenticationContext()
        {
            AuthenticationContext = new TestAuthenticationContext();
        }
    }
}
