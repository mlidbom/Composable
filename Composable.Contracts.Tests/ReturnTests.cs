using NUnit.Framework;

namespace Composable.Contracts.Tests
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
            Contract.ReturnValue(returnMe).NotNullEmptyOrWhiteSpace();
            return Contract.Return(returnMe, assert => assert.NotNullEmptyOrWhiteSpace());
        }
    }
}
