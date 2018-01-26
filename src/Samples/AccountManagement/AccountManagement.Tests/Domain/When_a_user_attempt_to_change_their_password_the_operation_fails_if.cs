using System;
using AccountManagement.Tests.Scenarios;
using Composable.Messaging.Buses;
using Composable.System.Linq;
using Composable.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Tests.Domain
{
    [TestFixture] public class When_a_user_attempt_to_change_their_password_the_operation_fails_if : AccountManagementTestBase
    {
        RegisterAccountScenario _registerAccountScenario;
        ChangePasswordScenario _changePasswordScenario;

        [SetUp] public void RegisterAccount()
        {
            _registerAccountScenario = new RegisterAccountScenario(ClientEndpoint);
            _registerAccountScenario.Execute();
            _changePasswordScenario = ChangePasswordScenario.Create(ClientEndpoint);
        }

        [Test] public void Password_is_null() => _changePasswordScenario.Mutate(@this => @this.NewPasswordAsString = null).Invoking(@this => @this.Execute()).ShouldThrow<Exception>();

        [Test] public void Password_empty_string() => _changePasswordScenario.Mutate(@this => @this.NewPasswordAsString = "").Invoking(@this => @this.Execute()).ShouldThrow<Exception>();

        [Test] public void OldPassword_is_null() => _changePasswordScenario.Mutate(@this => @this.OldPassword = null).Invoking(@this => @this.Execute()).ShouldThrow<Exception>();

        [Test] public void OldPassword_is_empty_string() => _changePasswordScenario.Mutate(@this => @this.OldPassword = "").Invoking(@this => @this.Execute()).ShouldThrow<Exception>();

        [Test] public void OldPassword_is_not_the_current_password_of_the_account() =>
            Host.AssertThatRunningScenarioThrowsBackendException<Exception>(() => _changePasswordScenario.Mutate(@this => @this.OldPassword = "Wrong").Execute())
            .Message.ToLower().Should().Contain("wrongpassword");
    }
}
