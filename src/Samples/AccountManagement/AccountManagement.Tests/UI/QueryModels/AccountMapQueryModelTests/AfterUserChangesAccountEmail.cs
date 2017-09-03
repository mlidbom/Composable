using AccountManagement.Tests.Scenarios;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Tests.UI.QueryModels.AccountMapQueryModelTests
{
    [TestFixture]
    public class AfterUserChangesAccountEmail : RegistersAccountDuringSetupTestBase
    {
        ChangeAccountEmailScenario _scenario;

        [SetUp]
        public void ChangeAccountEmail()
        {
            _scenario = new ChangeAccountEmailScenario(ServiceLocator, RegisteredAccount);
            _scenario.Execute();
            ReplaceContainerScope();
        }

        [Test]
        public void EmailIsTheOneFromTheEvent()
        {
            GetAccountQueryModel().Email.Should().Be(_scenario.NewEmail);
        }
    }
}
