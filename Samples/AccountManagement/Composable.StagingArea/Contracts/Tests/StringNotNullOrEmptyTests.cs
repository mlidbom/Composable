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
            string emptyString = "";
            Assert.Throws<StringIsEmptyException>(() => Contract.Optimized.Arguments(emptyString).NotNullOrEmpty());

            Assert.Throws<StringIsEmptyException>(() => Contract.Arguments(() => emptyString).NotNullOrEmpty());
        }

        [Test]
        public void UsesArgumentNameForExceptionMessage()
        {
            string emptyString = "";
            Assert.Throws<StringIsEmptyException>(() => Contract.Optimized.Argument(emptyString, "emptyString").NotNullOrEmpty())
                .Message.Should().Contain("emptyString");

            Assert.Throws<StringIsEmptyException>(() => Contract.Arguments(() => emptyString).NotNullOrEmpty())
                .Message.Should().Contain("emptyString");
        }
    }
}
