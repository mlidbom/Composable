using AccountManagement.UI.Commands.ValidationAttributes;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.UI.Commands.Tests
{
    [TestFixture]
    public class EmailAttributeTests
    {
        [Test]
        public void IsValidIfEmailIsNull()
        {
            new EmailAttribute().IsValid(null).Should().Be(true);
        }

        [Test]
        public void IsValidIfEmailIsEmpty()
        {
            new EmailAttribute().IsValid("").Should().Be(true);
        }

        [Test]
        public void IsNotValidIfEmailIsInvalid()
        {
            new EmailAttribute().IsValid("aoeustnh").Should().Be(false);
        }
    }
}