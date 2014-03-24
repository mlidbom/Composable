using System.Linq;
using AccountManagement.Domain.Events;
using AccountManagement.Domain.Shared;
using AccountManagement.TestHelpers.Scenarios;
using Composable.KeyValueStorage.Population;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests.AccountTests
{
    public class ChangePasswordSuccessScenarioTest : DomainTestBase
    {
        private Account _registeredAccount;
        private RegisterAccountScenario _registerAccountScenario;
        private const string NewPassword = "SomeComplexNewPassword1!";

        [SetUp]
        public void RegisterAccount()
        {
            _registerAccountScenario = new RegisterAccountScenario(Container);
            _registeredAccount = _registerAccountScenario.Execute();
            using(var transaction = Container.BeginTransactionalUnitOfWorkScope())
            {
                _registeredAccount.ChangePassword(_registerAccountScenario.PasswordAsString, new Password(NewPassword));
                transaction.Commit();
            }
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
                .Single().Password.AssertIsCorrectPassword(NewPassword);
        }

        [Test]
        public void AccountPasswordShouldAcceptTheNewPassword()
        {
            _registeredAccount.Password.AssertIsCorrectPassword(NewPassword);
        }
    }
}