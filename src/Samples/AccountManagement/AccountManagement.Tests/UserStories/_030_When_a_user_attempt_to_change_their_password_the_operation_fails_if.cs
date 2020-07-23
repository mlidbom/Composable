using System;
using Composable.SystemCE.LinqCE;
using FluentAssertions;
using JetBrains.Annotations;
using NUnit.Framework;

namespace AccountManagement.UserStories
{
    public class _030_When_a_user_attempt_to_change_their_password_the_operation_fails_if : UserStoryTest
    {
        [Test] public void New_password_is_invalid() =>
            TestData.Passwords.Invalid.All.ForEach(invalidPassword => Scenario.ChangePassword().WithNewPassword(invalidPassword).ExecutingShouldThrow<Exception>());

        [Test] public void OldPassword_is_null() => Scenario.ChangePassword().WithOldPassword(null).ExecutingShouldThrow<Exception>();

        [Test] public void OldPassword_is_empty_string() => Scenario.ChangePassword().WithOldPassword("").ExecutingShouldThrow<Exception>();

        [Test] public void OldPassword_is_not_the_current_password_of_the_account()
        {
            Scenario.ChangePassword().WithOldPassword("Wrong").ExecutingShouldThrow<Exception>().And.Message.ToLowerInvariant().Should().Contain("password").And.Contain("wrong");
            Host.AssertThrown<Exception>().Message.ToLowerInvariant().Should().Contain("password").And.Contain("wrong");
        }

        public _030_When_a_user_attempt_to_change_their_password_the_operation_fails_if([NotNull] string _) : base(_) {}
    }
}
