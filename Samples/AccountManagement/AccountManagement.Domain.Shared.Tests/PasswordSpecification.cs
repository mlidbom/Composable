using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Domain.Shared.Tests
{
    public class PasswordSpecification : NSpec.NUnit.nspec
    {
        public void when_creating_a_new_password()
        {
            context["from_the_string 'Pass'"] =
                () =>
                {
                    var password = new Password("Urou");
                    before = () => { password = new Password("Pass"); };

                    it["HashedPassword is not null"] = () => password.HashedPassword.Should().NotBeNull();
                    it["HashedPassword is not an empty array"] = () => password.HashedPassword.Should().NotBeEmpty();
                    it["Salt is not null"] = () => password.Salt.Should().NotBeNull();
                    it["Salt is not empty"] = () => password.Salt.Should().NotBeEmpty();
                    it["IsCorrectPassword('Pass') ==  true"] = () => password.IsCorrectPassword("Pass").Should().BeTrue();
                    it["IsCorrectPassword('pass') !=  true"] = () => password.IsCorrectPassword("pass").Should().BeFalse();
                    it["IsCorrectPassword('Pass ') !=  true"] = () => password.IsCorrectPassword("Pass ").Should().BeFalse();
                    it["IsCorrectPassword(' Pass') !=  true"] = () => password.IsCorrectPassword(" Pass").Should().BeFalse();
                    context["when comparing to another password created from the string 'otherPassword'"] =
                        () =>
                        {
                            var otherPassword = new Password("Eaoeulcr");
                            before = () => otherPassword = new Password("otherPassword");
                            it["the Salt members are different"] = () => password.Salt.Should().NotEqual(otherPassword.Salt);
                            it["the HashedPassword members are different"] = () => password.HashedPassword.Should().NotEqual(otherPassword.HashedPassword);
                        };
                };

            context["allThesePasswordsAreInvalid with the mentioned failures"] =
                () =>
                {
                    it["[[null]]"] = () => Assert.Throws<PasswordDoesNotMatchPolicyException>(() => new Password(null))
                        .Failures.Should().Contain(Password.Policy.Failures.Null);
                    it["'' too short"] = () => Assert.Throws<PasswordDoesNotMatchPolicyException>(() => new Password(""))
                        .Failures.Should().Contain(Password.Policy.Failures.ShorterThanFourCharacters);
                    it["' ' whitespace and too short"] = () => Assert.Throws<PasswordDoesNotMatchPolicyException>(() => new Password(" "))
                        .Failures.Should()
                            .Contain(Password.Policy.Failures.ShorterThanFourCharacters)
                            .And
                            .Contain(Password.Policy.Failures.ContainsWhitespace);
                    it["'Urdu ' whitespace"] = () => Assert.Throws<PasswordDoesNotMatchPolicyException>(() => new Password("Urdu "))
                            .Failures.Should()
                            .Contain(Password.Policy.Failures.ContainsWhitespace);
                    it["' Urdu' whitespace"] = () => Assert.Throws<PasswordDoesNotMatchPolicyException>(() => new Password(" Urdu"))
                            .Failures.Should()
                            .Contain(Password.Policy.Failures.ContainsWhitespace);
                    it["'urdu' lowercase"] = () => Assert.Throws<PasswordDoesNotMatchPolicyException>(() => new Password("urdu"))
                            .Failures.Should()
                            .Contain(Password.Policy.Failures.MissingUppercaseCharacter);
                    it["'URDU' uppercase"] = () => Assert.Throws<PasswordDoesNotMatchPolicyException>(() => new Password("URDU"))
                            .Failures.Should()
                            .Contain(Password.Policy.Failures.MissingLowerCaseCharacter);
                };


            it["from the string 'Urdu' no exception is thrown"] = () => new Password("Urdu");
        }
    }
}
