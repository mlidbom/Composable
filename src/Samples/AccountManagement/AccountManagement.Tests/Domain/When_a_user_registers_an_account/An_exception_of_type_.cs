using System;
using AccountManagement.Domain;
using AccountManagement.Tests.Scenarios;
using Composable.Messaging.Buses;
using Composable.System.Linq;
using Composable.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Tests.Domain.When_a_user_registers_an_account
{
    [TestFixture] public class An_exception_of_type_ : DomainTestBase
    {
        RegisterAccountScenario _registerAccountScenario;

        [SetUp] public void SetupWiringAndCreateRepositoryAndScope() { _registerAccountScenario = new RegisterAccountScenario(ClientBus); }

        [Test] public void DuplicateAccountException_is_thrown_if_email_is_already_registered()
        {
            _registerAccountScenario.Execute();
            Host.AssertThatRunningScenarioThrowsBackendException<DuplicateAccountException>(() => _registerAccountScenario.Execute())
                .Message.Should().Contain(_registerAccountScenario.Email);
        }

        [Test] public void CommandValidationFailureException_is_thrown_if_Password_is_null() =>
            AssertThrows.AggregateException<CommandValidationFailureException>(() => _registerAccountScenario.Mutate(@this => @this.Password = null).Execute());

        [Test] public void CommandValidationFailureException_is_thrown_if_Email_is_null()
            => AssertThrows.AggregateException<CommandValidationFailureException>(() => _registerAccountScenario.Mutate(@this => @this.Email = null).Execute());

        [Test] public void CommandValidationFailureException_is_thrown_if_AccountId_is_empty()
            => AssertThrows.AggregateException<CommandValidationFailureException>(() => _registerAccountScenario.Mutate(@this => @this.AccountId = Guid.Empty).Execute());
    }
}
