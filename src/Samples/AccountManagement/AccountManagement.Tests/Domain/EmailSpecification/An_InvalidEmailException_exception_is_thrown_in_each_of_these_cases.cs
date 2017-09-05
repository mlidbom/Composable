using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AccountManagement.Domain;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Tests.Domain.EmailSpecification
{
    [TestFixture] public class When_creating_a_new_email
    {
        [TestFixture] public class An_InvalidEmailException_containing_the_email_is_thrown_when_the_given_string_is
        {
            [Test, TestCaseSource(typeof(An_InvalidEmailException_containing_the_email_is_thrown_when_the_given_string_is), nameof(InvalidEmails))]
            public void in_each_of_these_cases_and_the_message_contains_the_email(string invalidEmail)
            {
                Assert.Throws<InvalidEmailException>(() => Email.Parse(invalidEmail))
                      .Message.Should()
                      .Contain(invalidEmail);
            }

            static IEnumerable InvalidEmails => new Dictionary<string, string>
                                                {
                                                    {"Only whitespace", " "},
                                                    {"No domain", "test.test.com"},
                                                    {"Repeated ..", "test.test@test..com"},
                                                    {"Repeated ...", "test.test@test...com"},
                                                    {"@.", "test.test@.test.dk"},
                                                    {"Repeated .. and @.", "test.test@..test.dk"},
                                                    {"Repeated .. and .@", "test.test..@test.dk"},
                                                    {".@", "test.test.@test.dk"}
                                                }.Select(entry => new TestCaseData(entry.Value).SetName($"\"{entry.Value}\" ({entry.Key})"));
        }

        [Test] public void An_InvalidEmailException_containing_the_string_null_is_thrown_if_the_string_passed_is_null()
            => Assert.Throws<InvalidEmailException>(() => Email.Parse(null)).Message.Should().Contain("null");

        [Test] public void An_InvalidEmailException_containing_an_empty_quotation_is_thrown_if_the_string_passed_is_null()
            => Assert.Throws<InvalidEmailException>(() => Email.Parse("")).Message.Should().Contain("''");
    }
}
