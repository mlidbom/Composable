using AccountManagement.UI.QueryModels.DocumentDb;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.UI.QueryModels.Tests.EmailToAccountMapQueryModelTests
{
    [TestFixture]
    public class AfterAccountIsRegistered : RegistersAccountDuringSetupAccountQueryModelTestBase
    {
        [Test]
        public void YouCanGetTheAccountViaTheEmail()
        {
            var reader = Container.Resolve<IAccountManagementDocumentDbReader>();
            var emailToAccountMap = reader.Get<EmailToAccountMapQueryModel>(RegisteredAccount.Email);

            emailToAccountMap.AccountId.Should().Be(RegisteredAccount.Id);
        }
    }
}
