using System.Linq;
using AccountManagement.Domain;
using FluentAssertions;
using JetBrains.Annotations;
using Xunit;
// ReSharper disable InconsistentNaming
// ReSharper disable ClassNeverInstantiated.Global

namespace AccountManagement.Tests.Domain
{
    [UsedImplicitly] public class PasswordSpecification
    {
        public class When_creating_a_password
        {
            public class From_the_string_Pass
            {
                readonly Password _password;
                public From_the_string_Pass() => _password = new Password("Pass");

                [Fact] void HashedPassword_is_not_null() => _password.HashedPassword.Should().NotBeNull();
                [Fact] void HashedPassword_is_not_an_empty_array() => _password.HashedPassword.Should().NotBeEmpty();
                [Fact] void Salt_is_not_null() => _password.Salt.Should().NotBeNull();

                public class IsCorrectPassword_is_ : From_the_string_Pass
                {
                    [Fact] void true_if_string_is_Pass() => _password.IsCorrectPassword("Pass").Should().BeTrue();

                    public class false_if : IsCorrectPassword_is_
                    {
                        [Fact] void _case_changes()
                        {
                            _password.IsCorrectPassword("pass").Should().BeFalse();
                            _password.IsCorrectPassword("PasS").Should().BeFalse();
                        }

                        [Fact] void space_is_prepended() => _password.IsCorrectPassword(" Pass").Should().BeFalse();
                        [Fact] void space_is_appended() => _password.IsCorrectPassword("Pass ").Should().BeFalse();
                    }
                }
            }

            public class a_PasswordDoesNotMatchPolicyException_with_matching_failure_is_thrown_when_password_
            {
                [Fact] void is_null() => AssertCreatingPasswordThrowsExceptionContainingFailure(null, Password.Policy.Failures.Null);
                [Fact] void is_shorter_than_four_characters() => AssertCreatingPasswordThrowsExceptionContainingFailure("abc", Password.Policy.Failures.ShorterThanFourCharacters);
                [Fact] void starts_with_whitespace() => AssertCreatingPasswordThrowsExceptionContainingFailure(" Pass", Password.Policy.Failures.BorderedByWhitespace);
                [Fact] void ends_with_whitespace() => AssertCreatingPasswordThrowsExceptionContainingFailure("Pass ", Password.Policy.Failures.BorderedByWhitespace);
                [Fact] void contains_only_lowercase_characters() => AssertCreatingPasswordThrowsExceptionContainingFailure("pass", Password.Policy.Failures.MissingUppercaseCharacter);
                [Fact] void contains_only_uppercase_characters() => AssertCreatingPasswordThrowsExceptionContainingFailure("PASS", Password.Policy.Failures.MissingLowerCaseCharacter);

                static void AssertCreatingPasswordThrowsExceptionContainingFailure(string password, Password.Policy.Failures expectedFailure)
                    => Assert.Throws<PasswordDoesNotMatchPolicyException>(() => new Password(password)).Failures.Should().Contain(expectedFailure);
            }
        }
    }
}
