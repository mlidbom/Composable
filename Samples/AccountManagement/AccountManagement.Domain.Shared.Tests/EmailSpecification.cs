using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Domain.Shared.Tests
{
    public class EmailSpecification : NSpec.NUnit.nspec
    {
        public void creating_new_email()
        {
            context["throws InvalidEmailException for all of these strings"] =
                () =>
                {
                    it["'null'"] = () => expect<InvalidEmailException>(() => Email.Parse(null));
                    it["''"] = expect<InvalidEmailException>(() => Email.Parse(null));
                    it["' '"] = expect<InvalidEmailException>(() => Email.Parse(" "));
                    it["'test.test.com' (No domain)"] = expect<InvalidEmailException>(() => Email.Parse("test.test.com"));
                    it["'test.test@test..com' (Repeated ..)"] = expect<InvalidEmailException>(() => Email.Parse("test.test@test..com"));
                    it["'test.test@test...com' (Repeated ...)"] = expect<InvalidEmailException>(() => Email.Parse("test.test@test...com"));
                    it["'test.test@.test.dk' (@.)"] = expect<InvalidEmailException>(() => Email.Parse("test.test@.test.dk"));
                    it["'test.test@..test.dk' (Repeated .. and @.)"] = expect<InvalidEmailException>(() => Email.Parse("test.test@..test.dk"));
                    it["'test.test..@test.dk' (Repeated .. and .@)"] = expect<InvalidEmailException>(() => Email.Parse("test.test..@test.dk"));
                    it["'test.test.@test.dk' (.@)"] = expect<InvalidEmailException>(() => Email.Parse("test.test.@test.dk"));
                };

            context["from the string 'brokenEmail'"] =
                () =>
                {
                    it["the exception message contains the text 'brokenEmail'"] =
                        () => Assert.Throws<InvalidEmailException>(() => Email.Parse("brokenEmail")).Message.Should().Contain("brokenEmail");
                };
            context["with string: 'test@test.dk'"] =
                () =>
                {
                    Email email = Email.Parse("t@t.to");
                    before = () => email = Email.Parse("test@test.dk");
                    it["ToString returns 'test@test.dk'"] = () => email.ToString().Should().Be("test@test.dk");
                };
        }
    }
}
