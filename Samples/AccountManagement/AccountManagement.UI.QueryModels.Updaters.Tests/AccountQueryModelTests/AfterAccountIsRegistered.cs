using AccountManagement.UI.QueryModels.DocumentDB.Readers.Services;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.UI.QueryModels.DocumentDB.Updaters.Tests.AccountQueryModelTests
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
