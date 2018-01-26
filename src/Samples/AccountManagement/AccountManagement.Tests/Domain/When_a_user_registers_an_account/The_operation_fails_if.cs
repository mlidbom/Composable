using System;
using AccountManagement.Tests.Scenarios;
using Composable.Messaging.Buses;
using Composable.System.Linq;
using Composable.Testing;
using NUnit.Framework;

namespace AccountManagement.Tests.Domain.When_a_user_registers_an_account
{
    [TestFixture] public class The_operation_fails_if : DomainTestBase
    {
        RegisterAccountScenario _registerAccountScenario;

        [SetUp] public void SetupWiringAndCreateRepositoryAndScope() { _registerAccountScenario = new RegisterAccountScenario(ClientEndpoint); }


        [Test] public void  Password_is_null() =>
            AssertThrows.Exception<CommandValidationFailureException>(() => _registerAccountScenario.Mutate(@this => @this.Password = null).Execute());

        [Test] public void  Password_is_empty() =>
            AssertThrows.Exception<CommandValidationFailureException>(() => _registerAccountScenario.Mutate(@this => @this.Password = "").Execute());

        [Test] public void Email_is_null()
            => AssertThrows.Exception<CommandValidationFailureException>(() => _registerAccountScenario.Mutate(@this => @this.Email = null).Execute());

        [Test] public void Email_is_empty_string()
            => AssertThrows.Exception<CommandValidationFailureException>(() => _registerAccountScenario.Mutate(@this => @this.Email = "").Execute());

        [Test] public void AccountId_is_empty()
            => AssertThrows.Exception<CommandValidationFailureException>(() => _registerAccountScenario.Mutate(@this => @this.AccountId = Guid.Empty).Execute());
    }
}
