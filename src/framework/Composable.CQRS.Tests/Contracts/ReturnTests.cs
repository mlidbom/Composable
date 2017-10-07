using Composable.Contracts;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.Contracts
{
    [TestFixture]
    public class ReturnTests
    {
        [Test]
        public void TestName()
        {
            Assert.Throws<ObjectIsNullContractViolationException>(() => ReturnInputStringAndRefuseToReturnNull(null));
            Assert.Throws<StringIsEmptyContractViolationException>(() => ReturnInputStringAndRefuseToReturnNull(""));
            Assert.Throws<StringIsWhitespaceContractViolationException>(() => ReturnInputStringAndRefuseToReturnNull(" ").Should().Be(""));
        }

        static string ReturnInputStringAndRefuseToReturnNull(string returnMe)
        {
            OldContract.ReturnValue(returnMe).NotNullEmptyOrWhiteSpace();
            return OldContract.Return(returnMe, assert => assert.NotNullEmptyOrWhiteSpace());
        }
    }
}
