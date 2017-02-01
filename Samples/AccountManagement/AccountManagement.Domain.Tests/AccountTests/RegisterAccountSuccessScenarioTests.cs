using System.Linq;
using AccountManagement.Domain.Events;
using AccountManagement.TestHelpers.Scenarios;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests.AccountTests
{
    [TestFixture]
    public class RegisterAccountSuccessScenarioTests : DomainTestBase
    {
        Account _registeredAccount;
        RegisterAccountScenario _registerAccountScenario;

        [SetUp]
        public void RegisterAccount()
        {
            _registerAccountScenario = new RegisterAccountScenario(Container);
            _registeredAccount = _registerAccountScenario.Execute();
        }

        [Test]
        public void AnIUserRegisteredAccountEventIsPublished()
        {
            MessageSpy.ReceivedMessages.OfType<IUserRegisteredAccountEvent>().ToList().Should().HaveCount(1);
        }

        [Test]
        public void AccountEmailIsTheOneUsedForRegistration()
        {
            Assert.That(_registeredAccount.Email, Is.EqualTo(_registerAccountScenario.Email));
        }

        [Test]
        public void AccountPasswordIsTheOnUsedForRegistration()
        {
            Assert.True(_registeredAccount.Password.IsCorrectPassword(_registerAccountScenario.PasswordAsString));
        }
    }
}
