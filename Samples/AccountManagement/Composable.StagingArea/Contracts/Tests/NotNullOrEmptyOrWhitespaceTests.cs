using System;
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
            Assert.Throws<NullValueException>(() => Contract.Argument(aNullString).NotNullEmptyOrWhiteSpace());
            // ReSharper restore ExpressionIsAlwaysNull
        }

        [Test]
        public void ThrowsStringIsEmptyArgumentExceptionForEmptyStrings()
        {
            Assert.Throws<StringIsEmptyException>(() => Contract.Argument(string.Empty).NotNullEmptyOrWhiteSpace());
        }

        [Test]
        public void ThrowsStringIsWhiteSpaceExceptionForStringConsistingOfTabsSpacesOrLineBreaks()
        {
            Assert.Throws<StringIsWhitespaceException>(() => Contract.Argument(" ").NotNullEmptyOrWhiteSpace());
            Assert.Throws<StringIsWhitespaceException>(() => Contract.Argument("\t").NotNullEmptyOrWhiteSpace());
            Assert.Throws<StringIsWhitespaceException>(() => Contract.Argument("\n").NotNullEmptyOrWhiteSpace());
            Assert.Throws<StringIsWhitespaceException>(() => Contract.Argument("\r\n").NotNullEmptyOrWhiteSpace());
            Assert.Throws<StringIsWhitespaceException>(() => Contract.Argument(Environment.NewLine).NotNullEmptyOrWhiteSpace());
        }
    }
}