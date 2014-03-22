using System;
using NUnit.Framework;

namespace Composable.Contracts.Tests
{
    [TestFixture]
    public class NotEmptyForGuidTests
    {
        [Test]
        public void NotEmptyThrowsArgumentExceptionForEmptyGuid()
        {
            Assert.Throws<ArgumentException>(() => Contract.Argument(Guid.Empty).NotEmpty());
        }
    }
}