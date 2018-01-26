using AccountManagement.Domain.Registration;
using AccountManagement.Scenarios;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.UserStories
{
    public class After_a_user_changes_their_email : UserStoryTest
    {
        ChangeAccountEmailScenario _changeEmailScenario;
        RegisterAccountScenario _registerAccountScenario;

        [SetUp] public void ChangeEmail()
        {
            _registerAccountScenario = new RegisterAccountScenario(ClientEndpoint);
            var (_, account) = _registerAccountScenario.Execute();
            _changeEmailScenario = new ChangeAccountEmailScenario(ClientEndpoint, account);
            _changeEmailScenario.Execute();
        }


        [Test] public void Logging_in_with_the_old_email__does_not_work() => new LoginScenario(ClientEndpoint, _changeEmailScenario.OldEmail.StringValue, _registerAccountScenario.Password).Execute().Succeeded.Should().Be(false);

        [Test] public void Logging_in_with_the_new_email_works() => new LoginScenario(ClientEndpoint, _changeEmailScenario.NewEmail, _registerAccountScenario.Password).Execute().Succeeded.Should().Be(true);

        [Test] public void Account_Email_is_the_new_email() => _changeEmailScenario.Account.Email.StringValue.Should().Be(_changeEmailScenario.NewEmail);

        [Test] public void Registering_an_account_with_the_old_email_works() => new RegisterAccountScenario(ClientEndpoint, email: _changeEmailScenario.OldEmail.ToString()).Execute();

        [Test] public void Attempting_to_register_an_account_with_the_new_email_fails_with_email_already_registered_message() =>
            new RegisterAccountScenario(ClientEndpoint, email: _changeEmailScenario.NewEmail).Execute()
            .Result.Status
            .Should().Be(RegistrationAttemptStatus.EmailAlreadyRegistered);
    }
}
