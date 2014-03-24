using System.Linq;
using AccountManagement.Domain.Events;
using AccountManagement.Domain.Shared;
using AccountManagement.TestHelpers;
using AccountManagement.TestHelpers.Fixtures;
using Castle.MicroKernel.Lifestyle;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests.AccountTests
{
    [TestFixture]
    public class RegisterAccountSuccessScenarioTests
    {
        private readonly Email _registrationEmail = Email.Parse("test.test@test.se");
        private MessageSpy _messageSpy;
        private Account _registeredAccount;
        private ValidAccountRegisteredFixture _validAccountRegisteredFixture;

        [SetUp]
        public void RegisterAccount()
        {
            _validAccountRegisteredFixture = new ValidAccountRegisteredFixture();
            var container = DomainTestWiringHelper.SetupContainerForTesting();
            using(container.BeginScope())
            {
              _messageSpy = container.Resolve<MessageSpy>();
              _registeredAccount = _validAccountRegisteredFixture.Setup(container);
            }
        }

        [Test]
        public void AnIUserRegisteredAccountEventIsPublished()
        {
            _messageSpy.ReceivedMessages.OfType<IUserRegisteredAccountEvent>().ToList().Should().HaveCount(1);
        }

        [Test]
        public void AccountEmailIsTheOneUsedForRegistration()
        {
            Assert.That(_registrationEmail, Is.EqualTo(_validAccountRegisteredFixture.Email));
        }

        [Test]
        public void AccountPasswordIsTheOnUsedForRegistration()
        {
            Assert.True(_registeredAccount.Password.IsCorrectPassword(_validAccountRegisteredFixture.PasswordAsString));
        }
    }
}
