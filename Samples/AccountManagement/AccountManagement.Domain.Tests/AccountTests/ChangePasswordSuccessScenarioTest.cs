using System.Linq;
using AccountManagement.Domain.Events;
using AccountManagement.TestHelpers.Scenarios;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests.AccountTests
{
    public class ChangePasswordSuccessScenarioTest : DomainTestBase
    {
        ChangePasswordScenario _changePasswordScenario;

        [SetUp]
        public void RegisterAccount()
        {
            _changePasswordScenario = new ChangePasswordScenario(Container);
            _changePasswordScenario.Execute();
        }

        [Test]
        public void AnIUserChangedAccountPasswordEventIsRaised()
        {
            MessageSpy.ReceivedMessages
                .OfType<IUserChangedAccountPasswordEvent>()
                .Should().HaveCount(1);
        }

        [Test]
        public void EventPasswordShouldAcceptTheUsedPasswordAsValid()
        {
            MessageSpy.ReceivedMessages.OfType<IUserChangedAccountPasswordEvent>()
                .Single().Password.AssertIsCorrectPassword(_changePasswordScenario.NewPasswordAsString);
        }

        [Test]
        public void AccountPasswordShouldAcceptTheNewPassword()
        {
            _changePasswordScenario.Account.Password.AssertIsCorrectPassword(_changePasswordScenario.NewPasswordAsString);
        }
    }
}
