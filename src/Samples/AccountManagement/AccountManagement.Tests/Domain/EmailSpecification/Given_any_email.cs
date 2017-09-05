using AccountManagement.Domain;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Tests.Domain.EmailSpecification
{
    [TestFixture] public class Given_any_email
    {
        [Test] public void ToString_returns_the_string_used_to_create_the_email() => Email.Parse("some.valid@email.com").ToString().Should().Be("some.valid@email.com");
    }
}
