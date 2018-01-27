using System;
using AccountManagement.Scenarios;
using Composable.System.Linq;
using NUnit.Framework;

namespace AccountManagement.UserStories
{
    public class _010_When_a_user_registers_an_account_the_operation_fails_if : UserStoryTest
    {
        RegisterAccountScenario _registerAccountScenario;

        [SetUp] public void SetupWiringAndCreateRepositoryAndScope() { _registerAccountScenario = new RegisterAccountScenario(ClientEndpoint); }

        [Test] public void Password_does_not_meet_policy() =>
            TestData.Passwords.Invalid.All.ForEach(invalidPassword => new RegisterAccountScenario(ClientEndpoint)
                                                                     .WithPassword(invalidPassword)
                                                                     .ExecutingShouldThrow<Exception>());

        [Test] public void Email_is_invalid() =>
            TestData.Emails.InvalidEmails.ForEach(invalidEmail => new RegisterAccountScenario(ClientEndpoint)
                                                                 .WithEmail(invalidEmail)
                                                                 .ExecutingShouldThrow<Exception>());

        [Test] public void Email_is_null()
            => _registerAccountScenario.WithEmail(null).ExecutingShouldThrow<Exception>();

        [Test] public void Email_is_empty_string()
            => _registerAccountScenario.WithEmail("").ExecutingShouldThrow<Exception>();

        [Test] public void AccountId_is_empty()
            => _registerAccountScenario.WithAccountId(Guid.Empty).ExecutingShouldThrow<Exception>();
    }
}
