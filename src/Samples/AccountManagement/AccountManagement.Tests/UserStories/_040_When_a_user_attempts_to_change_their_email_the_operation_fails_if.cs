using System;
using Composable.System.Linq;
using NUnit.Framework;

namespace AccountManagement.UserStories
{
    public class _040_When_a_user_attempts_to_change_their_email_the_operation_fails_if : UserStoryTest
    {
        [Test] public void NewEmail_is_invalid() =>
            TestData.Emails.InvalidEmails.ForEach(action: invalidEmail => Scenario.ChangeEmail()
                                                                                  .WithNewEmail(invalidEmail)
                                                                                  .ExecutingShouldThrow<Exception>());
    }
}
