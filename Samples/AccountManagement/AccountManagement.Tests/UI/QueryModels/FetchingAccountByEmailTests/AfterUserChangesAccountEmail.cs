using AccountManagement.Tests.Scenarios;
using AccountManagement.UI.QueryModels;
using AccountManagement.UI.QueryModels.Services;
using Composable.DependencyInjection;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Tests.UI.QueryModels.FetchingAccountByEmailTests
{
    [TestFixture]
    public class AfterUserChangesAccountEmail : RegistersAccountDuringSetupAccountQueryModelTestBase
    {
        ChangeAccountEmailScenario _scenario;

        [SetUp]
        public void ChangeEmail()
        {
            _scenario = new ChangeAccountEmailScenario(ServiceLocator, RegisteredAccount);
            _scenario.Execute();
            ReplaceContainerScope();//Changes are not expected to be visible in the same scope so start a new one.
        }

        [Test]
        public void YouCanGetTheAccountViaTheNewEmail()
        {
            AccountQueryModel account = null;
            ServiceLocator.Use<IAccountManagementQueryModelsReader>(useComponent: reader => reader
                .TryGetAccountByEmail(_scenario.NewEmail, out account)
                .Should().Be(expected: true));

            account.Id.Should().Be(RegisteredAccount.Id);
        }

        [Test]
        public void TryingToFetchViaTheOldEmailThrowsNoSuchDocumentException()
        {
            AccountQueryModel account = null;
            ServiceLocator.Use<IAccountManagementQueryModelsReader>(useComponent: reader => reader
                                                                   .TryGetAccountByEmail(_scenario.OldEmail, out account)
                                                                   .Should()
                                                                   .Be(expected: false));

            account.Should().Be(expected: null);
        }
    }
}
