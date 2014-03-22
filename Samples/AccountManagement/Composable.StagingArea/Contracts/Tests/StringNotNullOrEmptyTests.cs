using FluentAssertions;
using NUnit.Framework;

namespace Composable.Contracts.Tests
{
    [TestFixture]
    public class StringNotNullOrEmptyTests
    {
        [Test]
        public void NotEmptyThrowsStringIsEmptyArgumentExceptionForEmptyString()
        {
            Assert.Throws<StringIsEmptyException>(() => Contract.Arguments("").NotNullOrEmpty());
        }

        [Test]
        public void UsesArgumentNameForExceptionMessage()
        {
            Assert.Throws<StringIsEmptyException>(() => Contract.Argument("", "name").NotNullOrEmpty())
                .Message.Should().Contain("name");
        }
    }
}
