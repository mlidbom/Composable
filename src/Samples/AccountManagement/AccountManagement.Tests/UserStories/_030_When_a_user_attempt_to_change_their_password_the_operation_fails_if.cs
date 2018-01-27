using System;
using AccountManagement.Scenarios;
using Composable.Messaging.Buses;
using Composable.System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.UserStories
{
    [TestFixture] public class _030_When_a_user_attempt_to_change_their_password_the_operation_fails_if : UserStoryTest
    {
        RegisterAccountScenario _registerAccountScenario;
        ChangePasswordScenario _changePasswordScenario;

        [SetUp] public void RegisterAccount()
        {
            _registerAccountScenario = new RegisterAccountScenario(ClientEndpoint);
            _registerAccountScenario.Execute();
            _changePasswordScenario = ChangePasswordScenario.Create(ClientEndpoint);
        }

        [Test]public void New_password_does_not_meet_policy() =>
            TestData.Passwords.Invalid.All.ForEach(invalidPassword => ChangePasswordScenario.Create(ClientEndpoint).SetNewPassword(invalidPassword).Invoking(@this => @this.Execute()).ShouldThrow<Exception>());

        [Test] public void OldPassword_is_null() => _changePasswordScenario.SetOldPassword(null).Invoking(@this => @this.Execute()).ShouldThrow<Exception>();

        [Test] public void OldPassword_is_empty_string() => _changePasswordScenario.SetOldPassword("").Invoking(@this => @this.Execute()).ShouldThrow<Exception>();

        [Test] public void OldPassword_is_not_the_current_password_of_the_account() =>
            Host.AssertThatRunningScenarioThrowsBackendException<Exception>(() => _changePasswordScenario.Mutate(@this => @this.OldPassword = "Wrong").Execute())
                .Message.ToLower().Should().Contain("wrongpassword");
    }
}
