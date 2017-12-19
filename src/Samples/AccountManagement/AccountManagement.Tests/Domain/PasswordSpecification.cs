using AccountManagement.Domain;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Tests.Domain
{
    [TestFixture] public class PasswordSpecification
    {
        [TestFixture] public class When_creating_a_password
        {
            [TestFixture] public class From_the_string_Pass
            {
                readonly Password _password;
                public From_the_string_Pass() => _password = new Password("Pass");

                [Test] public void HashedPassword_is_not_null() => _password.HashedPassword.Should().NotBeNull();
                [Test] public void HashedPassword_is_not_an_empty_array() => _password.HashedPassword.Should().NotBeEmpty();
                [Test] public void Salt_is_not_null() => _password.Salt.Should().NotBeNull();

                [TestFixture] public class IsCorrectPassword_is_ : From_the_string_Pass
                {
                    [Test] public void true_if_string_is_Pass() => _password.IsCorrectPassword("Pass").Should().BeTrue();

                    public class false_if : IsCorrectPassword_is_
                    {
                        [Test] public void _case_changes()
                        {
                            _password.IsCorrectPassword("pass").Should().BeFalse();
                            _password.IsCorrectPassword("PasS").Should().BeFalse();
                        }

                        [Test] public void space_is_prepended() => _password.IsCorrectPassword(" Pass").Should().BeFalse();
                        [Test] public void space_is_appended() => _password.IsCorrectPassword("Pass ").Should().BeFalse();
                    }
                }
            }

            [TestFixture] public class a_PasswordDoesNotMatchPolicyException_with_matching_failure_is_thrown_when_password_
            {
                [Test] public void is_null() => AssertCreatingPasswordThrowsExceptionContainingFailure(null, Password.Policy.Failures.Null);
                [Test] public void is_shorter_than_four_characters() => AssertCreatingPasswordThrowsExceptionContainingFailure("abc", Password.Policy.Failures.ShorterThanFourCharacters);
                [Test] public void starts_with_whitespace() => AssertCreatingPasswordThrowsExceptionContainingFailure(" Pass", Password.Policy.Failures.BorderedByWhitespace);
                [Test] public void ends_with_whitespace() => AssertCreatingPasswordThrowsExceptionContainingFailure("Pass ", Password.Policy.Failures.BorderedByWhitespace);
                [Test] public void contains_only_lowercase_characters() => AssertCreatingPasswordThrowsExceptionContainingFailure("pass", Password.Policy.Failures.MissingUppercaseCharacter);
                [Test] public void contains_only_uppercase_characters() => AssertCreatingPasswordThrowsExceptionContainingFailure("PASS", Password.Policy.Failures.MissingLowerCaseCharacter);

                static void AssertCreatingPasswordThrowsExceptionContainingFailure(string password, Password.Policy.Failures expectedFailure)
                    // ReSharper disable once ObjectCreationAsStatement
                    => Assert.Throws<PasswordDoesNotMatchPolicyException>(() => new Password(password)).Failures.Should().Contain(expectedFailure);
            }
        }
    }
}
