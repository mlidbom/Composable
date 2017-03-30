using System.Linq;
using AccountManagement.Domain.Events;
using AccountManagement.TestHelpers.Scenarios;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests.After_a_user_has_registered_an_account
{
    [TestFixture]
    public class Then_ : DomainTestBase
    {
        Account _registeredAccount;
        RegisterAccountScenario _registerAccountScenario;

        [SetUp]
        public void RegisterAccount()
        {
            _registerAccountScenario = new RegisterAccountScenario(ServiceLocator);
            _registeredAccount = _registerAccountScenario.Execute();
        }

        [Test]
        public void An_IUserRegisteredAccountEvent_is_published()
        {
            MessageSpy.DispatchedMessages.OfType<AccountEvent.UserRegistered>().ToList().Should().HaveCount(1);
        }

        [Test]
        public void AccountEmail_is_the_one_used_for_registration()
        {
            Assert.That(_registeredAccount.Email, Is.EqualTo(_registerAccountScenario.Email));
        }

        [Test]
        public void AccountPassword_is_the_one_used_for_registration()
        {
            Assert.True(_registeredAccount.Password.IsCorrectPassword(_registerAccountScenario.PasswordAsString));
        }
    }
}
