using AccountManagement.API;
using AccountManagement.Domain.Registration;
using AccountManagement.Scenarios;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.UserStories
{
    [TestFixture] public class _020_After_a_user_has_registered_an_account : UserStoryTest
    {
        AccountResource _registeredAccount;
        RegisterAccountScenario _registerAccountScenario;
        AccountResource.Command.Register.RegistrationAttemptResult _result;

        [SetUp] public void RegisterAccount()
        {
            _registerAccountScenario = new RegisterAccountScenario(ClientEndpoint);
            (_result, _registeredAccount) = _registerAccountScenario.Execute();
            _result.Status.Should().Be(RegistrationAttemptStatus.Successful);
        }

        [Test] public void Login_with_the_correct_email_and_password_succeeds()
        {
            var result = new LoginScenario(ClientEndpoint, _registeredAccount, _registerAccountScenario.Password).Execute();

            result.Succeeded.Should().Be(true);
            result.AuthenticationToken.Should().NotBeNullOrWhiteSpace();
        }

        [Test] public void Login_with_the_correct_email_but_wrong_password_fails()
            => new LoginScenario(ClientEndpoint, _registeredAccount, "SomeOtherPassword").Execute().Succeeded.Should().Be(false);

        [Test] public void Login_with_the_wrong_email_but_correct_password_fails()
            => new LoginScenario(ClientEndpoint, "some_other@email.com", _registerAccountScenario.Password).Execute().Succeeded.Should().Be(false);

        [Test] public void Registering_another_account_with_the_same_email_fails_with_email_already_registered_message() =>
            new RegisterAccountScenario(ClientEndpoint, email: _registerAccountScenario.Email).Execute().Result.Status.Should().Be(RegistrationAttemptStatus.EmailAlreadyRegistered);

    }
}
