using System.Collections.Generic;
using Composable.Contracts;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.Contracts
{
    [TestFixture]
    public class StringNotNullOrEmptyTests
    {
        [Test]
        public void NotEmptyThrowsStringIsEmptyArgumentExceptionForEmptyString()
        {
            string emptyString = "";
            Assert.Throws<StringIsEmptyContractViolationException>(() => ContractTemp.Argument(() => emptyString).NotNullOrEmpty());
        }

        [Test]
        public void UsesArgumentNameForExceptionMessage()
        {
            string emptyString = "";
            Assert.Throws<StringIsEmptyContractViolationException>(() => ContractTemp.Argument(() => emptyString).NotNullOrEmpty())
                .Message.Should().Contain("emptyString");
        }

        [Test]
        public void ThrowsStringIsEmptyForEmptyStrings()
        {
            InspectionTestHelper.BatchTestInspection<StringIsEmptyContractViolationException, string>(
                inspected => inspected.NotNullOrEmpty(),
                badValues: new List<string> {"", ""},
                goodValues: new List<string> {"a", "aa", "aaa"});
        }

        [Test]
        public void ThrowsObjectIsNullForNullStrings()
        {
            InspectionTestHelper.BatchTestInspection<ObjectIsNullContractViolationException, string>(
                inspected => inspected.NotNullOrEmpty(),
                badValues: new List<string> {null, null},
                goodValues: new List<string> {"a", "aa", "aaa"});
        }
    }
}
