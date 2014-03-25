using AccountManagement.Web.Views.Account;
using NUnit.Framework;

namespace AccountManagement.Web.Tests.AccountControllerTests
{
    public class DisplayAccountInformationTests : AccountControllerTest
    {
        [Test]
        public void CanDisplayView()
        {
            var viewModel = (DisplayAccountDetailsViewModel)Controller.DisplayAccountDetails().Model;
        }
    }
}