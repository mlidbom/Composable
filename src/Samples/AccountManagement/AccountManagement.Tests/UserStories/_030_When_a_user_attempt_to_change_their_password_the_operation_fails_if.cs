using System;
using AccountManagement.API;
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
        AccountResource _account;

        [SetUp] public void RegisterAccount()
        {
            _registerAccountScenario = new RegisterAccountScenario(ClientEndpoint);
            _account = _registerAccountScenario.Execute().Account;
            _changePasswordScenario = ChangePasswordScenario.Create(ClientEndpoint);
        }

        [Test]public void New_password_does_not_meet_policy() =>
            TestData.Password.Invalid.All.ForEach(invalidPassword => new ChangePasswordScenario(ClientEndpoint, _account, oldPassword: _registerAccountScenario.Password, newPassword: invalidPassword).Invoking(@this => @this.Execute()).ShouldThrow<Exception>());

        [Test] public void OldPassword_is_null() => _changePasswordScenario.Mutate(@this => @this.OldPassword = null).Invoking(@this => @this.Execute()).ShouldThrow<Exception>();

        [Test] public void OldPassword_is_empty_string() => _changePasswordScenario.Mutate(@this => @this.OldPassword = "").Invoking(@this => @this.Execute()).ShouldThrow<Exception>();

        [Test] public void OldPassword_is_not_the_current_password_of_the_account() =>
            Host.AssertThatRunningScenarioThrowsBackendException<Exception>(() => _changePasswordScenario.Mutate(@this => @this.OldPassword = "Wrong").Execute())
                .Message.ToLower().Should().Contain("wrongpassword");
    }
}
