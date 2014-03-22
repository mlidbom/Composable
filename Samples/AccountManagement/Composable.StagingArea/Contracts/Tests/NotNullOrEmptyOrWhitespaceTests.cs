using System;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Contracts.Tests
{
    [TestFixture]
    public class NotNullOrEmptyOrWhitespaceTests
    {
        [Test]
        public void ThrowsArgumentNullForNullArguments()
        {
            String aNullString = null;
            // ReSharper disable ExpressionIsAlwaysNull
            Assert.Throws<ObjectIsNullException>(() => Contract.Optimized.Arguments(aNullString).NotNullEmptyOrWhiteSpace());
            // ReSharper restore ExpressionIsAlwaysNull
        }

        [Test]
        public void ThrowsStringIsEmptyArgumentExceptionForEmptyStrings()
        {
            Assert.Throws<StringIsEmptyException>(() => Contract.Optimized.Arguments(string.Empty).NotNullEmptyOrWhiteSpace());
        }

        [Test]
        public void ThrowsStringIsWhiteSpaceExceptionForStringConsistingOfTabsSpacesOrLineBreaks()
        {
            Assert.Throws<StringIsWhitespaceException>(() => Contract.Optimized.Arguments(" ").NotNullEmptyOrWhiteSpace());
            Assert.Throws<StringIsWhitespaceException>(() => Contract.Optimized.Arguments("\t").NotNullEmptyOrWhiteSpace());
            Assert.Throws<StringIsWhitespaceException>(() => Contract.Optimized.Arguments("\n").NotNullEmptyOrWhiteSpace());
            Assert.Throws<StringIsWhitespaceException>(() => Contract.Optimized.Arguments("\r\n").NotNullEmptyOrWhiteSpace());
            Assert.Throws<StringIsWhitespaceException>(() => Contract.Optimized.Arguments(Environment.NewLine).NotNullEmptyOrWhiteSpace());
        }

        [Test]
        public void ShouldUseArgumentNameForException()
        {
            Assert.Throws<StringIsWhitespaceException>(() => Contract.Optimized.Argument(Environment.NewLine, "name").NotNullEmptyOrWhiteSpace())
                .Message.Should().Contain("name");
        }
    }
}
