using FluentAssertions;
using NUnit.Framework;

namespace Composable.Contracts.Tests
{
    [TestFixture]
    public class NotEmpty
    {
        [Test]
        public void NotEmptyThrowsStringIsEmptyArgumentExceptionForEmptyString()
        {
            Assert.Throws<StringIsEmptyException>(() => Contract.Arguments("").NotEmpty());
        }

        [Test]
        public void UsesArgumentNameForExceptionMessage()
        {
            Assert.Throws<StringIsEmptyException>(() => Contract.Argument("", "name").NotEmpty())
                .Message.Should().Be("name");
        }
    }
}
