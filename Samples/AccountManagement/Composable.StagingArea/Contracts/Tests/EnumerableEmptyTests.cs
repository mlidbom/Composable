using System.Collections.Generic;
using NUnit.Framework;

namespace Composable.Contracts.Tests
{
    [TestFixture]
    public class EnumerableEmptyTests
    {
        [Test]
        public void ThrowsEnumerableIsEmptyException()
        {
            var list = new List<string>();
            Assert.Throws<EnumerableIsEmptyException>(() => Contract.Argument(list).NotNullOrEmpty());
        }
    }
}
