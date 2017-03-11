using System;
using AccountManagement.TestHelpers.Scenarios;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests.After_a_user_has_registered_an_account
{
    [TestFixture]
    public class When_they_attempt_to_change_their_password_an_exception_is_thrown_if_ : DomainTestBase
    {
        RegisterAccountScenario _registerAccountScenario;
        ChangePasswordScenario _changePasswordScenario;

        [SetUp]
        public void RegisterAccount()
        {
            _registerAccountScenario = new RegisterAccountScenario(Container);
            _registerAccountScenario.Execute();
            _changePasswordScenario = new ChangePasswordScenario(Container);
        }

        [Test]
        public void Password_is_null()
        {
            _changePasswordScenario.NewPassword = null;
            _changePasswordScenario
                .Invoking(scenario => scenario.Execute())
                .ShouldThrow<Exception>();
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
            _changePasswordScenario
                .Invoking(scenario => scenario.Execute())
                .ShouldThrow<Exception>();
        }
    }
}
