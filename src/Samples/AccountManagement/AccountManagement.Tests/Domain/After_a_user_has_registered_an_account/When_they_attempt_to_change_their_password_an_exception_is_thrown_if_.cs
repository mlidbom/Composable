using System;
using System.Threading.Tasks;
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

        [SetUp] public async Task RegisterAccount()
        {
            _registerAccountScenario = new RegisterAccountScenario(ClientBus);
            await _registerAccountScenario.ExecuteAsync();
            _changePasswordScenario = await ChangePasswordScenario.CreateAsync(ClientBus);
        }

        [Test] public async Task Password_is_null()
        {
            _changePasswordScenario.NewPasswordAsString = null;
            await AssertThrows.Async<Exception>(() => _changePasswordScenario.ExecuteAsync());
        }

        [Test] public async Task OldPassword_is_null() =>
            await AssertThrows.Async<Exception>(() => _changePasswordScenario.Mutate(@this => @this.OldPassword = null).ExecuteAsync());

        [Test] public async Task OldPassword_is_empty_string() =>
            await AssertThrows.Async<Exception>(() => _changePasswordScenario.Mutate(@this => @this.OldPassword = "").ExecuteAsync());

        [Test] public async Task OldPassword_is_not_the_current_password_of_the_account() =>
            (await Host.AssertThatRunningScenarioThrowsBackendExceptionAsync<Exception>(() => _changePasswordScenario.Mutate(@this => @this.OldPassword = "Wrong").ExecuteAsync()))
            .Message.ToLower().Should().Contain("wrongpassword");
    }
}
