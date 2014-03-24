using System.Linq;
using AccountManagement.Domain.Events;
using AccountManagement.TestHelpers.Fixtures;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests.AccountTests
{
    [TestFixture]
    public class RegisterAccountSuccessScenarioTests : DomainTestBase
    {
        private Account _registeredAccount;
        private ValidAccountRegisteredFixture _accountFixture;

        [SetUp]
        public void RegisterAccount()
        {
            _accountFixture = new ValidAccountRegisteredFixture();
            _registeredAccount = _accountFixture.Setup(Container);
        }

        [Test]
        public void AnIUserRegisteredAccountEventIsPublished()
        {
            MessageSpy.ReceivedMessages.OfType<IUserRegisteredAccountEvent>().ToList().Should().HaveCount(1);
        }

        [Test]
        public void AccountEmailIsTheOneUsedForRegistration()
        {
            Assert.That(_registeredAccount.Email, Is.EqualTo(_accountFixture.Email));
        }

        [Test]
        public void AccountPasswordIsTheOnUsedForRegistration()
        {
            Assert.True(_registeredAccount.Password.IsCorrectPassword(_accountFixture.PasswordAsString));
        }
    }
}
