using AccountManagement.TestHelpers.Scenarios;
using AccountManagement.UI.QueryModels.Services;
using Castle.MicroKernel.Lifestyle;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.UI.QueryModels.Tests.FetchingAccountByEmailTests
{
    [TestFixture]
    public class AfterUserChangesAccountEmail : RegistersAccountDuringSetupAccountQueryModelTestBase
    {
        private ChangeAccountEmailScenario _scenario;

        [SetUp]
        public void ChangeEmail()
        {
            _scenario = new ChangeAccountEmailScenario(Container, RegisteredAccount);
            _scenario.Execute();
        }

        [Test]
        public void YouCanGetTheAccountViaTheNewEmail()
        {
            using(Container.BeginScope())
            {
                AccountQueryModel account;
                Container.Resolve<IAccountManagementQueryModelsReader>()
                    .TryGetAccountByEmail(_scenario.NewEmail, out account)
                    .Should().Be(true);

                account.Id.Should().Be(RegisteredAccount.Id);
            }
        }

        [Test]
        public void TryingToFetchViaTheOldEmailThrowsNoSuchDocumentException()
        {
            using(Container.BeginScope())
            {
                AccountQueryModel account;
                Container.Resolve<IAccountManagementQueryModelsReader>()
                    .TryGetAccountByEmail(_scenario.OldEmail, out account)
                    .Should().Be(false);

                account.Should().Be(null);
            }
        }
    }
}
