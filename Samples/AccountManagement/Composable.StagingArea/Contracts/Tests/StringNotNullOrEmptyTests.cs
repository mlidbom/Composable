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
            Assert.Throws<StringIsEmptyException>(() => Contract.ArgumentsOptimized("").NotNullOrEmpty());
        }

        [Test]
        public void UsesArgumentNameForExceptionMessage()
        {
            Assert.Throws<StringIsEmptyException>(() => Contract.ArgumentOptimized("", "name").NotNullOrEmpty())
                .Message.Should().Contain("name");
        }
    }
}
