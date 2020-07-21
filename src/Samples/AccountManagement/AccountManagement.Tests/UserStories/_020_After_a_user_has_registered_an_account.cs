using AccountManagement.Domain.Registration;
using AccountManagement.UserStories.Scenarios;
using FluentAssertions;
using JetBrains.Annotations;
using NUnit.Framework;

namespace AccountManagement.UserStories
{
    public class _020_After_a_user_has_registered_an_account : UserStoryTest
    {
        RegisterAccountScenario _registerAccountScenario;

        [SetUp] public void RegisterAccount()
        {
            _registerAccountScenario = Scenario.Register;
            var result= _registerAccountScenario.Execute().Result;
            result.Status.Should().Be(RegistrationAttemptStatus.Successful);
        }

        [Test] public void Login_with_the_correct_email_and_password_succeeds()
        {
            var result = Scenario.Login(_registerAccountScenario).Execute();

            result.Succeeded.Should().Be(true);
            result.AuthenticationToken.Should().NotBeNullOrWhiteSpace();
        }

        [Test] public void Login_with_the_correct_email_but_wrong_password_fails()
            => Scenario.Login(_registerAccountScenario).WithPassword("SomeOtherPassword").Execute().Succeeded.Should().Be(false);

        [Test] public void Login_with_the_wrong_email_but_correct_password_fails()
            => Scenario.Login(_registerAccountScenario).WithEmail("some_other@email.com").Execute().Succeeded.Should().Be(false);

        [Test] public void Registering_another_account_with_the_same_email_fails_with_email_already_registered_message() =>
            Scenario.Register.WithEmail(_registerAccountScenario.Email).Execute().Result.Status.Should().Be(RegistrationAttemptStatus.EmailAlreadyRegistered);

        public _020_After_a_user_has_registered_an_account([NotNull] string _) : base(_) {}
    }
}
