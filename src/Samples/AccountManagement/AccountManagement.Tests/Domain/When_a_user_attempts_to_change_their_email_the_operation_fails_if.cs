using System;
using AccountManagement.Tests.Scenarios;
using Composable.System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Tests.Domain
{
    public class When_a_user_attempts_to_change_their_email_the_operation_fails_if : AccountManagementTestBase
    {
        ChangeAccountEmailScenario _changeEmail;

        [SetUp] public void RegisterAccount() => _changeEmail = ChangeAccountEmailScenario.Create(ClientEndpoint);

        [Test] public void NewEmail_is_null() =>
            _changeEmail.Mutate(@this => @this.NewEmail = null).Invoking(@this => @this.Execute()).ShouldThrow<Exception>();

        [Test] public void NewEmail_is_empty_string() =>
            _changeEmail.Mutate(@this => @this.NewEmail = "").Invoking(@this => @this.Execute()).ShouldThrow<Exception>();
    }
}
