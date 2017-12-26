using System;
using System.Threading.Tasks;
using AccountManagement.Tests.Scenarios;
using Composable.Messaging.Buses;
using Composable.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Tests.Domain.After_a_user_has_registered_an_account
{
    [TestFixture]
    public class When_they_attempt_to_change_their_password_an_exception_is_thrown_if_ : DomainTestBase
    {
        RegisterAccountScenario _registerAccountScenario;
        ChangePasswordScenario _changePasswordScenario;

        [SetUp]
        public async Task RegisterAccount()
        {
            _registerAccountScenario = new RegisterAccountScenario(ClientBus);
            await _registerAccountScenario.ExecuteAsync();
            _changePasswordScenario = await ChangePasswordScenario.Create(ClientBus);
        }

        [Test]
        public void Password_is_null()
        {
            _changePasswordScenario.NewPasswordAsString = null;
            AssertThrows.Exception<Exception>(() => _changePasswordScenario.Execute());
        }

        [Test]
        public void OldPassword_is_null()
        {
            _changePasswordScenario.OldPassword = null;
            _changePasswordScenario
                .Invoking(scenario => scenario.Execute())
                .ShouldThrow<Exception>();
        }

        [Test]
        public void OldPassword_is_empty_string()
        {
            _changePasswordScenario.OldPassword = "";
            _changePasswordScenario
                .Invoking(scenario => scenario.Execute())
                .ShouldThrow<Exception>();
        }

        [Test]
        public void OldPassword_is_not_the_current_password_of_the_account()
        {
            _changePasswordScenario.OldPassword = "Wrong";

            Host.AssertThatRunningScenarioThrowsBackendException<Exception>(() => _changePasswordScenario.Execute())
                .Message.ToLower().Should().Contain("wrongpassword");
        }
    }
}
