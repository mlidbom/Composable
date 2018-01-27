using System;
using AccountManagement.Scenarios;
using Composable.System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.UserStories
{
    public class _010_When_a_user_registers_an_account_the_operation_fails_if : UserStoryTest
    {
        RegisterAccountScenario _registerAccountScenario;

        [SetUp] public void SetupWiringAndCreateRepositoryAndScope() { _registerAccountScenario = new RegisterAccountScenario(ClientEndpoint); }

        [Test] public void Password_does_not_meet_policy() =>
            TestData.Password.Invalid.All.ForEach(invalidPassword => new RegisterAccountScenario(ClientEndpoint)
                                                                    .Mutate(@this => @this.Password = invalidPassword)
                                                                    .Invoking(@this => @this.Execute())
                                                                    .ShouldThrow<Exception>());

        [Test] public void Email_is_null()
            => _registerAccountScenario.Mutate(@this => @this.Email = null).Invoking(@this => @this.Execute()).ShouldThrow<Exception>();

        [Test] public void Email_is_empty_string()
            => _registerAccountScenario.Mutate(@this => @this.Email = "").Invoking(@this => @this.Execute()).ShouldThrow<Exception>();

        [Test] public void AccountId_is_empty()
            => _registerAccountScenario.Mutate(@this => @this.AccountId = Guid.Empty).Invoking(@this => @this.Execute()).ShouldThrow<Exception>();
    }
}
