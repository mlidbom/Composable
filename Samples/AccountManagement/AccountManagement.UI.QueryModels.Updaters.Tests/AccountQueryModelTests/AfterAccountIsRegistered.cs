using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.UI.QueryModels.Updaters.Tests.AccountQueryModelTests
{
    [TestFixture]
    public class AfterAccountIsRegistered : RegistersAccountDuringSetupAccountQueryModelTestBase
    {
        [Test]
        public void QueryModelExists()
        {
            GetQueryModel();
        }

        [Test]
        public void EmailIsTheSameAsTheOneInTheAccount()
        {
            GetQueryModel().Email.Should().Be(RegisteredAccount.Email);
        }

        [Test]
        public void PasswordMatchesTheDomainObject()
        {
            GetQueryModel().Password.Should().Be(RegisteredAccount.Password);
        }
    }
}
