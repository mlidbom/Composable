using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.UI.QueryModels.EventStore.Generators.Tests.EmailToAccountMapQueryModelTests
{
    [TestFixture]
    public class AfterAccountIsRegistered : RegistersAccountDuringSetupTestBase
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
