using NUnit.Framework;

namespace Composable.Contracts.Tests
{
    [TestFixture]
    public class ReturnTests
    {
        [Test]
        public void TestName()
        {
            Assert.Throws<ObjectIsNullException>(() => ReturnInputStringAndRefuseToReturnNull(null));
            Assert.Throws<StringIsEmptyException>(() => ReturnInputStringAndRefuseToReturnNull(""));
            Assert.Throws<StringIsWhitespaceException>(() => ReturnInputStringAndRefuseToReturnNull(" "));
        }

        public string ReturnInputStringAndRefuseToReturnNull(string returnMe)
        {
            return Contract.Return(returnMe, assert => assert.NotNullEmptyOrWhiteSpace());
        }
    }
}
