using Composable.Contracts;
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
            Assert.Throws<StringIsWhitespaceContractViolationException>(() => ReturnInputStringAndRefuseToReturnNull(" "));
        }

        string ReturnInputStringAndRefuseToReturnNull(string returnMe)
        {
            Contract.ReturnValue(returnMe).NotNullEmptyOrWhiteSpace();
            return Contract.Return(returnMe, assert => assert.NotNullEmptyOrWhiteSpace());
        }
    }
}
