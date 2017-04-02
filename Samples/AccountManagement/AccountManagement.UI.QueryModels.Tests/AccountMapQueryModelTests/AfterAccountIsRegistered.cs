using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.UI.QueryModels.Tests.AccountMapQueryModelTests
{
    [TestFixture]
    public class AfterAccountIsRegistered : RegistersAccountDuringSetupTestBase
    {
        [Test]
        public void QueryModelExists()
        {
            GetAccountQueryModel();
        }

        [Test]
        public void EmailIsTheSameAsTheOneInTheAccount()
        {
            GetAccountQueryModel().Email.Should().Be(RegisteredAccount.Email);
        }

        [Test]
        public void PasswordMatchesTheDomainObject()
        {
            GetAccountQueryModel().Password.ShouldBeEquivalentTo(RegisteredAccount.Password);
        }
    }
}
