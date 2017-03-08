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

        public string ReturnInputStringAndRefuseToReturnNull(string returnMe)
        {
            ContractTemp.ReturnValue(returnMe).NotNullEmptyOrWhiteSpace();
            return ContractTemp.Return(returnMe, assert => assert.NotNullEmptyOrWhiteSpace());
        }
    }
}
