using System;
using AccountManagement.Domain;
using AccountManagement.Domain.Services;
using AccountManagement.Tests.Scenarios;
using Composable.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Tests.Domain.When_a_user_registers_an_account
{
    [TestFixture]
    public class An_exception_of_type_ : DomainTestBase
    {
        RegisterAccountScenario _registerAccountScenario;

        [SetUp]
        public void SetupWiringAndCreateRepositoryAndScope()
        {
            _registerAccountScenario = new RegisterAccountScenario(ClientBus);
        }

        [Test]
        public void DuplicateAccountException_is_thrown_if_email_is_already_registered()
        {
            _registerAccountScenario.Execute();
            AssertThrows.Exception<Exception>(() => _registerAccountScenario.Execute());
            Host.AssertThrown<DuplicateAccountException>();
        }

        [Test]
        public void ObjectIsNullContractViolationException_is_thrown_if_Password_is_null()
        {
            _registerAccountScenario.Command.Password = null;
            this.Invoking(_ =>_registerAccountScenario.Execute())
                .ShouldThrow<Exception>();
        }

        [Test]
        public void ObjectIsNullContractViolationException_is_thrown_if_Email_is_null()
        {
            _registerAccountScenario.Command.Email = null;
            this.Invoking(_ => _registerAccountScenario.Execute())
                .ShouldThrow<Exception>();
        }

        [Test]
        public void ObjectIsDefaultContractViolationException_is_thrown_if_AccountId_is_empty()
        {
            _registerAccountScenario.Command.AccountId = Guid.Empty;
            this.Invoking(_ => _registerAccountScenario.Execute())
                .ShouldThrow<Exception>();
        }
    }
}
