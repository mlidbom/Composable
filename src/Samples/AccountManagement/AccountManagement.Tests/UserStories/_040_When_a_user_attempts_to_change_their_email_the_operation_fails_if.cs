using System;
using AccountManagement.Scenarios;
using Composable.System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.UserStories
{
    public class _040_When_a_user_attempts_to_change_their_email_the_operation_fails_if : UserStoryTest
    {
        ChangeAccountEmailScenario _changeEmail;

        [SetUp] public void RegisterAccount() => _changeEmail = ChangeAccountEmailScenario.Create(ClientEndpoint);

        [Test] public void NewEmail_is_null() =>
            _changeEmail.SetNewEmail(null).Invoking(@this => @this.Execute()).ShouldThrow<Exception>();

        [Test] public void NewEmail_is_empty_string() =>
            _changeEmail.SetNewEmail("").Invoking(@this => @this.Execute()).ShouldThrow<Exception>();
    }
}
