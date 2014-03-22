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
            Assert.Throws<StringIsEmptyException>(() => Contract.Optimized.Arguments("").NotNullOrEmpty());
        }

        [Test]
        public void UsesArgumentNameForExceptionMessage()
        {
            Assert.Throws<StringIsEmptyException>(() => Contract.Optimized.Argument("", "name").NotNullOrEmpty())
                .Message.Should().Contain("name");
        }
    }
}
