using System;
using AccountManagement.Tests.Scenarios;
using Composable.Messaging.Buses;
using Composable.System.Linq;
using Composable.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Tests.Domain.After_a_user_has_registered_an_account
{
    [TestFixture] public class When_they_attempt_to_change_their_password_an_exception_is_thrown_if_ : DomainTestBase
    {
        RegisterAccountScenario _registerAccountScenario;
        ChangePasswordScenario _changePasswordScenario;

        [SetUp] public void RegisterAccount()
        {
            _registerAccountScenario = new RegisterAccountScenario(DomainEndpoint);
            _registerAccountScenario.Execute();
            _changePasswordScenario = ChangePasswordScenario.Create(DomainEndpoint);
        }

        [Test] public void Password_is_null()
        {
            _changePasswordScenario.NewPasswordAsString = null;
            AssertThrows.Exception<Exception>(() => _changePasswordScenario.Execute());
        }

        [Test] public void OldPassword_is_null() =>
            AssertThrows.Exception<Exception>(() => _changePasswordScenario.Mutate(@this => @this.OldPassword = null).Execute());

        [Test] public void OldPassword_is_empty_string() =>
            AssertThrows.Exception<Exception>(() => _changePasswordScenario.Mutate(@this => @this.OldPassword = "").Execute());

        [Test] public void OldPassword_is_not_the_current_password_of_the_account() =>
            Host.AssertThatRunningScenarioThrowsBackendException<Exception>(() => _changePasswordScenario.Mutate(@this => @this.OldPassword = "Wrong").Execute())
            .Message.ToLower().Should().Contain("wrongpassword");
    }
}
