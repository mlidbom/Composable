using FluentAssertions;

namespace AccountManagement.Domain.Shared.Tests
{
    public class PasswordSpecification : NSpec.NUnit.nspec
    {
        public void when_creating_a_password_from_the_string()
        {
            Password password = new Password("a");

            context["password"] =
                () =>
                {
                    before =
                        () => { password = new Password("password"); };
                    it["HashedPassword is not null"] = () => password.HashedPassword.Should().NotBeNull();
                    it["HashedPassword is not an empty array"] = () => password.HashedPassword.Should().NotBeEmpty();
                    it["Salt is not null"] = () => password.Salt.Should().NotBeNull();
                    it["Salt is not empty"] = () => password.Salt.Should().NotBeEmpty();
                    it["IsCorrectPassword('password') ==  true"] = () => password.IsCorrectPassword("password").Should().BeTrue();
                };
        }
    }
}
