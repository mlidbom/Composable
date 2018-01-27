using AccountManagement.UserStories.Scenarios;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.UserStories
{
    public class _031_After_a_user_changes_their_password : UserStoryTest
    {
        ChangePasswordScenario _changePasswordScenario;

        [SetUp] public void RegisterAccount()
        {
            _changePasswordScenario = Scenario.ChangePassword();
            _changePasswordScenario.Execute();
        }

        [Test] public void Logging_in_with_the_new_password_works() =>
            Scenario.Login(_changePasswordScenario.Account.Email, _changePasswordScenario.NewPassword).Execute().Succeeded.Should().Be(true);

        [Test] public void Logging_in_with_the_old_password_fails() =>
            Scenario.Login(_changePasswordScenario.Account.Email, _changePasswordScenario.OldPassword).Execute().Succeeded.Should().Be(false);
    }
}
