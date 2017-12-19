using AccountManagement.Domain;
using FluentAssertions;
using NUnit.Framework;
// ReSharper disable InconsistentNaming

namespace AccountManagement.Tests.Domain
{
    static class PasswordSpecification
    {
        static class When_creating_a_password
        {
            static class From_the_string_Pass
            {
                static readonly Password _password = new Password("Pass");

                [Test] public static void HashedPassword_is_not_null() => _password.HashedPassword.Should().NotBeNull();
                [Test] public static void HashedPassword_is_not_an_empty_array() => _password.HashedPassword.Should().NotBeEmpty();
                [Test] public static void Salt_is_not_null() => _password.Salt.Should().NotBeNull();

                [TestFixture] public static class IsCorrectPassword_is_
                {
                    [Test] public static void true_if_string_is_Pass() => _password.IsCorrectPassword("Pass").Should().BeTrue();

                    public static class false_if
                    {
                        [Test] public static void _case_changes()
                        {
                            _password.IsCorrectPassword("pass").Should().BeFalse();
                            _password.IsCorrectPassword("PasS").Should().BeFalse();
                        }

                        [Test] public static void space_is_prepended() => _password.IsCorrectPassword(" Pass").Should().BeFalse();
                        [Test] public static void space_is_appended() => _password.IsCorrectPassword("Pass ").Should().BeFalse();
                    }
                }
            }

            static class a_PasswordDoesNotMatchPolicyException_with_matching_failure_is_thrown_when_password_
            {
                [Test] public static void Is_null() => AssertCreatingPasswordThrowsExceptionContainingFailure(null, Password.Policy.Failures.Null);
                [Test] public static void Is_shorter_than_four_characters() => AssertCreatingPasswordThrowsExceptionContainingFailure("abc", Password.Policy.Failures.ShorterThanFourCharacters);
                [Test] public static void Starts_with_whitespace() => AssertCreatingPasswordThrowsExceptionContainingFailure(" Pass", Password.Policy.Failures.BorderedByWhitespace);
                [Test] public static void Ends_with_whitespace() => AssertCreatingPasswordThrowsExceptionContainingFailure("Pass ", Password.Policy.Failures.BorderedByWhitespace);
                [Test] public static void Contains_only_lowercase_characters() => AssertCreatingPasswordThrowsExceptionContainingFailure("pass", Password.Policy.Failures.MissingUppercaseCharacter);
                [Test] public static void Contains_only_uppercase_characters() => AssertCreatingPasswordThrowsExceptionContainingFailure("PASS", Password.Policy.Failures.MissingLowerCaseCharacter);

                static void AssertCreatingPasswordThrowsExceptionContainingFailure(string password, Password.Policy.Failures expectedFailure)
                    // ReSharper disable once ObjectCreationAsStatement
                    => Assert.Throws<PasswordDoesNotMatchPolicyException>(() => new Password(password)).Failures.Should().Contain(expectedFailure);
            }
        }
    }
}
