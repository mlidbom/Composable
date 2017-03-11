using AccountManagement.TestHelpers.Scenarios;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.UI.QueryModels.Tests.AccountMapQueryModelTests
{
    [TestFixture]
    public class AfterUserChangesAccountEmail : RegistersAccountDuringSetupTestBase
    {
        ChangeAccountEmailScenario _scenario;

        [SetUp]
        public void ChangeAccountEmail()
        {
            _scenario = new ChangeAccountEmailScenario(Container, RegisteredAccount);
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
