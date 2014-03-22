using NUnit.Framework;

namespace Composable.Contracts.Tests
{
    [TestFixture]
    public class NotEmpty
    {
        [Test]
        public void NotEmptyThrowsStringIsEmptyArgumentExceptionForEmptyString()
        {
            Assert.Throws<StringIsEmptyException>(() => Contract.Argument("").NotEmpty());
        }
    }
}