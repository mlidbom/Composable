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
            Assert.Throws<ObjectIsNullException>(() => Contract.Arguments(aNullString).NotNullEmptyOrWhiteSpace());
            // ReSharper restore ExpressionIsAlwaysNull
        }

        [Test]
        public void ThrowsStringIsEmptyArgumentExceptionForEmptyStrings()
        {
            Assert.Throws<StringIsEmptyException>(() => Contract.Arguments(string.Empty).NotNullEmptyOrWhiteSpace());
        }

        [Test]
        public void ThrowsStringIsWhiteSpaceExceptionForStringConsistingOfTabsSpacesOrLineBreaks()
        {
            Assert.Throws<StringIsWhitespaceException>(() => Contract.Arguments(" ").NotNullEmptyOrWhiteSpace());
            Assert.Throws<StringIsWhitespaceException>(() => Contract.Arguments("\t").NotNullEmptyOrWhiteSpace());
            Assert.Throws<StringIsWhitespaceException>(() => Contract.Arguments("\n").NotNullEmptyOrWhiteSpace());
            Assert.Throws<StringIsWhitespaceException>(() => Contract.Arguments("\r\n").NotNullEmptyOrWhiteSpace());
            Assert.Throws<StringIsWhitespaceException>(() => Contract.Arguments(Environment.NewLine).NotNullEmptyOrWhiteSpace());
        }

        [Test]
        public void ShouldUseArgumentNameForException()
        {
            Assert.Throws<StringIsWhitespaceException>(() => Contract.Argument(Environment.NewLine, "name").NotNullEmptyOrWhiteSpace())
                .Message.Should().Contain("name");
        }
    }
}
