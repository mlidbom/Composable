using AccountManagement.UI.Web.Controllers;
using NUnit.Framework;

namespace AccountManagement.UI.Web.Tests.AccountControllerTests
{
    public abstract class AccountControllerTest : AuthenticatedControllerTest
    {
        protected AccountController Controller;

        [SetUp]
        public void CreateController()
        {
            Controller = Container.Resolve<AccountController>();
        }
    }
}
