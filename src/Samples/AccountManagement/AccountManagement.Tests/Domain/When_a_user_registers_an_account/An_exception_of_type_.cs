using System;
using System.Threading.Tasks;
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

        [Test] public async Task DuplicateAccountException_is_thrown_if_email_is_already_registered()
        {
            await _registerAccountScenario.ExecuteAsync();
            Host.AssertThatRunningScenarioThrowsBackendException<DuplicateAccountException>(() => _registerAccountScenario.ExecuteAsync().Wait())
                .Message.Should().Contain(_registerAccountScenario.Email);
        }

        [Test] public async Task  CommandValidationFailureException_is_thrown_if_Password_is_null() =>
            await AssertThrows.Async<CommandValidationFailureException>(() => _registerAccountScenario.Mutate(@this => @this.Password = null).ExecuteAsync());

        [Test] public async Task  CommandValidationFailureException_is_thrown_if_Email_is_null()
            => await AssertThrows.Async<CommandValidationFailureException>(() => _registerAccountScenario.Mutate(@this => @this.Email = null).ExecuteAsync());

        [Test] public async Task CommandValidationFailureException_is_thrown_if_AccountId_is_empty()
            => await AssertThrows.Async<CommandValidationFailureException>(() => _registerAccountScenario.Mutate(@this => @this.AccountId = Guid.Empty).ExecuteAsync());
    }
}
