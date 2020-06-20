using AccountManagement.Domain;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.UnitTests.Emails
{
    [TestFixture] public class When_creating_a_new_email
    {
        [TestFixture] public class An_InvalidEmailException_containing_the_email_is_thrown_if_email_
        {
            [Test, TestCaseSource(typeof(TestData.Emails), nameof(TestData.Emails.InvalidEmailsTestData))]
            public void In_each_of_these_cases_and_the_message_contains_the_email(string invalidEmail)
            {
                var invalidEmailException = Assert.Throws<InvalidEmailException>(() => Email.Parse(invalidEmail));

                if(!string.IsNullOrEmpty(invalidEmail))
                {
                    invalidEmailException.Message.Should().Contain((invalidEmail));
                }
            }
        }

        [Test] public void An_InvalidEmailException_containing_the_string_null_is_thrown_if_the_string_passed_is_null()
            => Assert.Throws<InvalidEmailException>(() => Email.Parse(null)).Message.Should().Contain("null");

        [Test] public void An_InvalidEmailException_containing_an_empty_quotation_is_thrown_if_the_string_passed_is_null()
            => Assert.Throws<InvalidEmailException>(() => Email.Parse("")).Message.Should().Contain("''");
    }
}
