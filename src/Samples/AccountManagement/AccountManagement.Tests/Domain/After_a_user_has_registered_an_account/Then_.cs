using System.Linq;
using AccountManagement.API;
using AccountManagement.Domain.Events;
using AccountManagement.Tests.Scenarios;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Tests.Domain.After_a_user_has_registered_an_account
{
    [TestFixture] public class Then_ : DomainTestBase
    {
        AccountResource _registeredAccount;
        RegisterAccountScenario _registerAccountScenario;
        AccountResource.Command.Register.RegistrationAttemptResult _result;

        [SetUp] public void RegisterAccount()
        {
            _registerAccountScenario = new RegisterAccountScenario(ClientBus);
            (_result, _registeredAccount) = _registerAccountScenario.Execute();
            _result.Should().Be(AccountResource.Command.Register.RegistrationAttemptResult.Successful);
        }

        [Test] public void An_IUserRegisteredAccountEvent_is_published() => MessageSpy.DispatchedMessages.OfType<AccountEvent.UserRegistered>().ToList().Should().HaveCount(1);

        [Test] public void AccountEmail_is_the_one_used_for_registration() => Assert.That(_registeredAccount.Email.ToString(), Is.EqualTo(_registerAccountScenario.Email));

        [Test] public void AccountPassword_is_the_one_used_for_registration() => Assert.True(_registeredAccount.Password.IsCorrectPassword(_registerAccountScenario.Password));

        [Test] public void Login_with_the_correct_email_and_password_succeeds_returning_NotNullOrWhiteSpace_authenticationToken()
        {
            var result = new LoginScenario(ClientBus, _registeredAccount, _registerAccountScenario.Password).Execute();

            result.Succeeded.Should().Be(true);
            result.AuthenticationToken.Should().NotBeNullOrWhiteSpace();
        }

        [Test] public void Login_with_the_correct_email_but_wrong_password_fails()
            => new LoginScenario(ClientBus, _registeredAccount, "SomeOtherPassword").Execute()
               .Succeeded.Should().Be(false);

        [Test] public void Login_with_the_wrong_email_but_correct_password_fails()
            => new LoginScenario(ClientBus, "some_other@email.com", _registerAccountScenario.Password).Execute()
               .Succeeded.Should().Be(false);

        [Test]
        public void Attempting_to_register_an_account_with_the_new_email_fails_with_email_already_registered_message()
        {
            var scenario = new RegisterAccountScenario(ClientBus, email: _registerAccountScenario.Email);

            var (result, _) = scenario.Execute();
            result.Should().Be(AccountResource.Command.Register.RegistrationAttemptResult.EmailAlreadyRegistered);
        }
    }
}
