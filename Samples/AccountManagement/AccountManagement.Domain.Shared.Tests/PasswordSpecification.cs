using System;
using FluentAssertions;

namespace AccountManagement.Domain.Shared.Tests
{
    public class PasswordSpecification : NSpec.NUnit.nspec
    {
        public void when_creating_a_new_password()
        {
            context["from_the_string 'password'"] =
                () =>
                {
                    var password = new Password("a");
                    before = () => { password = new Password("password"); };

                    it["HashedPassword is not null"] = () => password.HashedPassword.Should().NotBeNull();
                    it["HashedPassword is not an empty array"] = () => password.HashedPassword.Should().NotBeEmpty();
                    it["Salt is not null"] = () => password.Salt.Should().NotBeNull();
                    it["Salt is not empty"] = () => password.Salt.Should().NotBeEmpty();
                    it["IsCorrectPassword('password') ==  true"] = () => password.IsCorrectPassword("password").Should().BeTrue();
                    it["IsCorrectPassword('Password') !=  true"] = () => password.IsCorrectPassword("Password").Should().BeFalse();
                    it["IsCorrectPassword('password ') !=  true"] = () => password.IsCorrectPassword("password ").Should().BeFalse();
                    it["IsCorrectPassword(' password') !=  true"] = () => password.IsCorrectPassword(" password").Should().BeFalse();                    
                    context["when comparing to another password created from the string 'otherPassword'"] = 
                        () =>
                        {
                            var otherPassword = new Password(Guid.NewGuid().ToString());
                            before = () => otherPassword = new Password("otherPassword");
                            it["the Salt members are different"] = () => password.Salt.Should().NotEqual(otherPassword.Salt);
                            it["the HashedPassword members are different"] = () => password.HashedPassword.Should().NotEqual(otherPassword.HashedPassword);
                        };
                };
            it["from the string '' an exception is thrown"] = expect<InvalidPasswordException>(() => new Password(""));
            it["from the string ' ' an exception is thrown"] = expect<InvalidPasswordException>(() => new Password(" "));
            it["from the string 'a' no exception is thrown"] = () => new Password("a");
        }
    }
}
