using AccountManagement.TestHelpers.Scenarios;
using Composable.Contracts;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests.AccountTests
{
    public class ChangeEmailFailureScenariosTests : DomainTestBase
    {
        private ChangeAccountEmailScenario _changeEmail;

        [SetUp]
        public void RegisterAccount()
        {
            _changeEmail = new ChangeAccountEmailScenario(Container);
        }

        [Test]
        public void WhenEmailIsNullObjectIsNullExceptionIsThrown()
        {
            _changeEmail.NewEmail = null;
            _changeEmail.Invoking(scenario => scenario.Execute())
                .ShouldThrow<ObjectIsNullContractViolationException>();
        }
    }
}
