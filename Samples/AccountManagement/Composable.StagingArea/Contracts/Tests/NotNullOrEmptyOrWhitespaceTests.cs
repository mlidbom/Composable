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
            Assert.Throws<ObjectIsNullException>(() => Contract.ArgumentsOptimized(aNullString).NotNullEmptyOrWhiteSpace());
            // ReSharper restore ExpressionIsAlwaysNull
        }

        [Test]
        public void ThrowsStringIsEmptyArgumentExceptionForEmptyStrings()
        {
            Assert.Throws<StringIsEmptyException>(() => Contract.ArgumentsOptimized(string.Empty).NotNullEmptyOrWhiteSpace());
        }

        [Test]
        public void ThrowsStringIsWhiteSpaceExceptionForStringConsistingOfTabsSpacesOrLineBreaks()
        {
            Assert.Throws<StringIsWhitespaceException>(() => Contract.ArgumentsOptimized(" ").NotNullEmptyOrWhiteSpace());
            Assert.Throws<StringIsWhitespaceException>(() => Contract.ArgumentsOptimized("\t").NotNullEmptyOrWhiteSpace());
            Assert.Throws<StringIsWhitespaceException>(() => Contract.ArgumentsOptimized("\n").NotNullEmptyOrWhiteSpace());
            Assert.Throws<StringIsWhitespaceException>(() => Contract.ArgumentsOptimized("\r\n").NotNullEmptyOrWhiteSpace());
            Assert.Throws<StringIsWhitespaceException>(() => Contract.ArgumentsOptimized(Environment.NewLine).NotNullEmptyOrWhiteSpace());
        }

        [Test]
        public void ShouldUseArgumentNameForException()
        {
            Assert.Throws<StringIsWhitespaceException>(() => Contract.ArgumentOptimized(Environment.NewLine, "name").NotNullEmptyOrWhiteSpace())
                .Message.Should().Contain("name");
        }
    }
}
