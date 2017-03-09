using FluentAssertions;
using Machine.Specifications;
using NUnit.Framework;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMember.Local
namespace AccountManagement.Domain.Shared.Tests
{
    public class When_creating_an_email
    {
        static InvalidEmailException AssertEmailThrowsException(string email) => Assert.Throws<InvalidEmailException>(() => Email.Parse(email));

        class an_InvalidEmailException_is_thrown_when_string
        {
            It is_null = () => AssertEmailThrowsException(null);
            It is_empty = () => AssertEmailThrowsException("");
            It is_space = () => AssertEmailThrowsException(" ");
            It has_no_at_character = () => AssertEmailThrowsException("test.test.com");
            It has_repeated_dot_character = () => AssertEmailThrowsException("test.test@test..com");
            It has_triple_repeated_dot_character = () => AssertEmailThrowsException("test.test@test...com");
            It has_dot_directly_after_at = () => AssertEmailThrowsException("test.test@.test.dk");
            It has_repeated_dot_and_dot_after_at = () => AssertEmailThrowsException("test.test@..test.dk");
            It has_dot_directly_before_at = () => AssertEmailThrowsException("test.test.@test.dk");
        }

        class from_the_string_brokenEmail
        {
            It the_exception_message_contains_the_string_brokenEmail =
                () => AssertEmailThrowsException("brokenEmail").Message.Should().Contain("brokenEmail");
        }

        class from_the_string_testATtestDOTdk
        {
            It toString_returns_testATtestDOTdk = () => Email.Parse("test@test.dk").ToString().Should().Be("test@test.dk");
        }
    }
}
