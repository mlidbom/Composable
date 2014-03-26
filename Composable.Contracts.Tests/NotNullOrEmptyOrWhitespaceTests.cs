using System;
using System.Collections.Generic;
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
            Assert.Throws<ObjectIsNullContractViolationException>(() => Contract.Arguments(() => aNullString).NotNullEmptyOrWhiteSpace());
            // ReSharper restore ExpressionIsAlwaysNull
        }

        [Test]
        public void ThrowsStringIsEmptyArgumentExceptionForEmptyStrings()
        {
            Assert.Throws<StringIsEmptyContractViolationException>(() => Contract.Arguments(() => string.Empty).NotNullEmptyOrWhiteSpace());
        }

        [Test]
        public void ThrowsStringIsWhiteSpaceExceptionForStringConsistingOfTabsSpacesOrLineBreaks()
        {
            var space = " ";
            var tab = "\t";
            var lineBreak = "\n";
            var newLine = "\r\n";
            var environmentNewLine = Environment.NewLine;
            Assert.Throws<StringIsWhitespaceContractViolationException>(() => Contract.Arguments(() => space).NotNullEmptyOrWhiteSpace());
            Assert.Throws<StringIsWhitespaceContractViolationException>(() => Contract.Arguments(() => tab).NotNullEmptyOrWhiteSpace());
            Assert.Throws<StringIsWhitespaceContractViolationException>(() => Contract.Arguments(() => lineBreak).NotNullEmptyOrWhiteSpace());
            Assert.Throws<StringIsWhitespaceContractViolationException>(() => Contract.Arguments(() => newLine).NotNullEmptyOrWhiteSpace());
            Assert.Throws<StringIsWhitespaceContractViolationException>(() => Contract.Arguments(() => environmentNewLine).NotNullEmptyOrWhiteSpace());

            Assert.Throws<StringIsWhitespaceContractViolationException>(() => Contract.Arguments(() => environmentNewLine, () => space).NotNullEmptyOrWhiteSpace());


            var badValues = new List<string> {space, tab, lineBreak, newLine, environmentNewLine};
            var goodValues = new List<string> {"aoeu", "lorem"};

            InspectionTestHelper.BatchTestInspection<StringIsWhitespaceContractViolationException, string>(
                assert: inspected => inspected.NotNullEmptyOrWhiteSpace(),
                badValues: badValues,
                goodValues: goodValues);
        }

        [Test]
        public void ShouldUseArgumentNameForException()
        {
            var newLine = Environment.NewLine;
            Assert.Throws<StringIsWhitespaceContractViolationException>(() => Contract.Arguments(()  => newLine).NotNullEmptyOrWhiteSpace())
                .Message.Should().Contain("newLine");
        }
    }
}
