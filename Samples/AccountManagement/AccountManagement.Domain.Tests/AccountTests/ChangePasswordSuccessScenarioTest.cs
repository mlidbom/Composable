using System.Linq;
using AccountManagement.Domain.Events;
using AccountManagement.Domain.Shared;
using AccountManagement.TestHelpers.Fixtures;
using Composable.KeyValueStorage.Population;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests.AccountTests
{
    public class ChangePasswordSuccessScenarioTest : DomainTestBase
    {
        private Account _registeredAccount;
        private AccountRegisteredFixture _accountRegisteredFixture;
        private const string NewPassword = "SomeComplexNewPassword1!";

        [SetUp]
        public void RegisterAccount()
        {
            _accountRegisteredFixture = new AccountRegisteredFixture();
            _registeredAccount = _accountRegisteredFixture.Setup(Container);
            using(var transaction = Container.BeginTransactionalUnitOfWorkScope())
            {
                _registeredAccount.ChangePassword(_accountRegisteredFixture.PasswordAsString, new Password(NewPassword));
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