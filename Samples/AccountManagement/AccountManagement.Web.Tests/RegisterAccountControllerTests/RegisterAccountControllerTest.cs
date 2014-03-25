using AccountManagement.Web.Controllers;
using NUnit.Framework;

namespace AccountManagement.Web.Tests.RegisterAccountControllerTests
{
    public abstract class RegisterAccountControllerTest : MvcControllerTest
    {
        protected RegisterAccountController Controller;

        [SetUp]
        public void CreateController()
        {
            Controller = Container.Resolve<RegisterAccountController>();
        }
    }
}