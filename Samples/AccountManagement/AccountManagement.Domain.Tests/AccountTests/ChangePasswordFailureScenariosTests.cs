using System;
using AccountManagement.Domain.Shared;
using AccountManagement.TestHelpers.Scenarios;
using Composable.Contracts;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests.AccountTests
{
    [TestFixture]
    public class ChangePasswordFailureScenariosTests : DomainTestBase
    {
        private RegisterAccountScenario _registerAccountScenario;
        private Account _account;
        private ChangePasswordScenario _changePasswordScenario;

        [SetUp]
        public void RegisterAccount()
        {
            _registerAccountScenario = new RegisterAccountScenario(Container);
            _account = _registerAccountScenario.Execute();
            _changePasswordScenario = new ChangePasswordScenario(Container);
        }

        [Test]
        public void WhenNewPasswordIsNullObjectNullExceptionIsThrown()
        {
            _changePasswordScenario.NewPassword = null;
            _changePasswordScenario
                .Invoking(scenario => scenario.Execute())
                .ShouldThrow<Exception>();
        }

        [Test]
        public void WhenOldPasswordIsNullObjectNullExceptionIsThrown()
        {
            _changePasswordScenario.OldPassword = null;
            _changePasswordScenario
                .Invoking(scenario => scenario.Execute())
                .ShouldThrow<Exception>();
        }

        [Test]
        public void WhenOldPasswordIsEmptyStringIsEmptyExceptionIsThrown()
        {
            _changePasswordScenario.OldPassword = "";
            _changePasswordScenario
                .Invoking(scenario => scenario.Execute())
                .ShouldThrow<Exception>();
        }

        [Test]
        public void WhenOldPasswordIsIncorrectWrongPasswordExceptionIsThrown()
        {
            _changePasswordScenario.OldPassword = "Wrong";
            _changePasswordScenario
                .Invoking(scenario => scenario.Execute())
                .ShouldThrow<Exception>();
        }
    }
}
