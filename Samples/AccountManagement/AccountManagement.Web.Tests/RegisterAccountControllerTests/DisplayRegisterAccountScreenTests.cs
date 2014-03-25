using AccountManagement.Web.Views.RegisterAccount;
using NUnit.Framework;

namespace AccountManagement.Web.Tests.RegisterAccountControllerTests
{
    public class DisplayRegisterAccountScreenTests : RegisterAccountControllerTest
    {
        [Test]
        public void CanDisplayView()
        {
            var viewModel = (DisplayAccountRegistrationViewModel)Controller.DisplayAccountRegistrationView().Model;
        }
    }
}